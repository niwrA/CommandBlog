using NoteCommandFactoryShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NoteControllerShared
{
    public interface INoteViewModel
    {
        string Text { get; set; }
        Guid Guid { get; set; }
        DateTime CreatedOn { get; set; }
        ObservableCollection<INoteCommand> Commands { get; set; }
        void UpdateText(string text, bool asNotify);
        void RemoveText(int offset, int length, bool asNotify);
    }

    public class NoteChangedEventArgs : PropertyChangedEventArgs
    {
        public NoteChangedEventArgs(string propertyName) : base(propertyName)
        {
        }
        public NoteChangedEventArgs(INoteViewModel note, string propertyName) : base(propertyName)
        {
            Note = note;
        }
        public INoteViewModel Note { get; set; }
    }

    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
    public class NoteModelBase : NotifyPropertyChangedBase, INoteViewModel
    {
        public ObservableCollection<INoteCommand> Commands { get; set; } = new ObservableCollection<INoteCommand>();

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public Guid Guid { get; set; }

        private string _text = "";
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }
        public void UpdateText(string text, bool asNotify)
        {
            if (asNotify)
            {
                Text = text;
            }
            else
            {
                _text = text;
            }
        }

        public void RemoveText(int offset, int length, bool asNotify)
        {
            if (asNotify)
            {
                Text = Text.Remove(offset, length);
            }
            else
            {
                _text = _text.Remove(offset, length);
            }
        }
    }
    public class NoteViewModel : NoteModelBase
    {
        public NoteViewModel() { }

        public NoteViewModel(string text, Guid guid)
        {
            this.Text = text;
            this.Guid = guid;
        }

        public NoteViewModel(NoteEditModel noteEdit)
        {
            Text = noteEdit.Text;
            Guid = noteEdit.Guid;
            this.Commands = noteEdit.Commands;
        }

        public NoteViewModel(string text)
        {
            Text = text;
            Guid = Guid.NewGuid();
        }
        public void UpdateText(string text)
        {
            Text = text;
        }
    }

    public class NoteEditModel : NoteModelBase
    {
        public bool IsEditComplete { get; private set; }
        public bool IsEditStart { get; private set; } = true;
        public string OriginalText { get; private set; }
        public NoteEditModel(NoteViewModel note)
        {
            Guid = note.Guid;
            Text = note.Text;
            OriginalText = note.Text;
            Commands = note.Commands;
        }
        public NoteEditModel() { }

        internal void EditComplete()
        {
            IsEditComplete = true;
        }
        internal void FirstEditHandled()
        {
            IsEditStart = false;
        }
    }


    public class NotesController : NotifyPropertyChangedBase
    {
        public event EventHandler<NoteChangedEventArgs> NoteChanged;

        private INoteCommandsController _commandFactory;
        public NotesController(INoteCommandsController commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public ObservableCollection<INoteViewModel> DisplayNotes { get; set; } = new ObservableCollection<INoteViewModel>();
        public ObservableCollection<INoteCommand> AllCommands { get; private set; } = new ObservableCollection<INoteCommand>();
        public ObservableCollection<INoteCommand> DisplayCommands { get; private set; } = new ObservableCollection<INoteCommand>();

        private NoteModelBase _selectedNote;

        public void CreateNote(string text)
        {
            var newNote = new NoteViewModel(text);
            ProcessCreateNote(text, newNote);
        }

        private void ProcessCreateNote(string text, NoteViewModel newNote)
        {
            DisplayNotes.Insert(0, newNote);
            var noteCommand = _commandFactory.Create(newNote.Guid, text);
            AddCommand(noteCommand, newNote);
        }

        public INoteViewModel CreateNote(string text, Guid guid)
        {
            var newNote = new NoteViewModel(text, guid);
            ProcessCreateNote(text, newNote);
            return newNote;
        }

        private void AddCommand(INoteCommand noteCommand, INoteViewModel note)
        {
            AllCommands.Insert(0, noteCommand);
            DisplayCommands.Insert(0, noteCommand);
            note.Commands.Insert(0, noteCommand);
        }

        public void DeleteNote(Guid guid)
        {
            var noteToDelete = DisplayNotes.Single(w => w.Guid == guid);
            DisplayNotes.Remove(noteToDelete);
            var noteCommand = _commandFactory.Delete(noteToDelete.Guid);
            AddCommand(noteCommand, noteToDelete as NoteModelBase);
        }

        public void PostTextChanges(INoteViewModel note, ICollection<TextChange> changes, string orgText)
        {
            if (note is NoteEditModel)
            {
                var noteEdit = note as NoteEditModel;
                if (!noteEdit.IsEditComplete) // prevent triggers from changes from Edit to View models
                {
                    foreach (var change in changes)
                    {
                        // the order matters - if we select something and then start overwriting that,
                        // we have a delete and add in one change.
                        if (change.RemovedLength > 0)
                        {
                            var text = orgText.Substring(change.Offset, change.RemovedLength);
                            RemoveText(noteEdit, change.Offset, text, false);
                        }
                        if (change.AddedLength > 0)
                        {
                            var text = note.Text.Substring(change.Offset, change.AddedLength);
                            InsertText(noteEdit, change.Offset, text, false);
                        }
                    }
                }
            }
        }


        // only needed when we don't use textchange
        public void UpdateNote(NoteEditModel noteEdit)
        {
            var note = new NoteViewModel(noteEdit);

            note.UpdateText(noteEdit.Text);
            var noteCommand = _commandFactory.Update(note.Guid, noteEdit.Text);
            AddCommand(noteCommand, note);
        }

        // swap EditModel back to ViewModel
        internal void ConfirmEdit(NoteEditModel editedNote)
        {
            editedNote.EditComplete();
            var note = new NoteViewModel(editedNote);
            DisplayNotes[DisplayNotes.IndexOf(editedNote)] = note;
        }


        internal void ShowAllNotes()
        {
            DisplayCommands.Clear();
            foreach (var command in AllCommands.OrderByDescending(o => o.CreatedOn))
            {
                DisplayCommands.Add(command);
            }
        }

        internal void RemoveText(INoteViewModel note, int offset, string text, bool asNotify)
        {
            var updatedText = note.Text; // already updated at this point if coming from UI
            var command = _commandFactory.DeleteText(note.Guid, offset, text);
            AddCommand(command, note);
            note.UpdateText(updatedText, false); // update without notify to prevent loop
        }

        internal void InsertText(INoteViewModel note, int offset, string text, bool asNotify)
        {
            var updatedText = note.Text; // not yet updated at this point
            updatedText = InsertText(note, offset, text, updatedText);
            var command = _commandFactory.InsertText(note.Guid, offset, text);
            AddCommand(command, note);

            note.UpdateText(updatedText, false); // update without notify to prevent loop

        }

        private string InsertText(INoteViewModel note, int offset, string text, string updatedText)
        {
            if (note.Text.Length == offset)
            {
                updatedText += text;
            }
            else
            {
                updatedText = note.Text.Insert(offset, text);
            }

            return updatedText;
        }

        internal void SelectNote(INoteViewModel note)
        {
            DisplayCommands.Clear();
            foreach (var command in note.Commands)
            {
                DisplayCommands.Add(command);
            }
        }

        internal void EditNote(NoteViewModel note)
        {
            var editNote = new NoteEditModel(note);
            DisplayNotes[DisplayNotes.IndexOf(note)] = editNote;
        }

        internal void ReplayAllCommands()
        {
            // reverse
            var playBackCommands = new List<INoteCommand>();
            foreach (var command in AllCommands)
            {
                if (playBackCommands.Count == 0)
                {
                    playBackCommands.Add(command);
                }
                else
                {
                    playBackCommands.Insert(0, command);
                }
            }

            this.DisplayNotes.Clear();
            AllCommands.Clear();
            DisplayCommands.Clear();

            foreach (var command in playBackCommands)
            {
                Task.Delay(250).Wait();
                ReplayCommand(command);
            }
        }

        private void ReplayCommand(INoteCommand command)
        {
            if (command is CreateNoteCommand)
            {
                var createNoteCmd = command as CreateNoteCommand;
                var note = CreateNote(createNoteCmd.Text, createNoteCmd.NoteGuid) as NoteViewModel;
                note.PropertyChanged += Note_PropertyChanged;
            }
            else
            {
                var note = DisplayNotes.Single(w => w.Guid == command.NoteGuid);
                var text = note.Text;
                if (command is InsertTextIntoNoteCommand)
                {
                    var insertTextCmd = command as InsertTextIntoNoteCommand;
                    InsertText(note, insertTextCmd.Offset, insertTextCmd.Text, true);
                }
                else if (command is DeleteTextFromNoteCommand)
                {
                    var deleteTextCmd = command as DeleteTextFromNoteCommand;
                    RemoveText(note, deleteTextCmd.Offset, deleteTextCmd.Text, true);
                }
            }
        }

        private void Note_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {            
            var note = sender as NoteModelBase;
            NoteChanged?.Invoke(this, new NoteChangedEventArgs(note, e.PropertyName));
        }
    }
}
