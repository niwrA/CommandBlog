using NoteCommandFactoryShared;
using NoteControllerShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Note
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotesController vm = new NotesController(new NoteCommandsController());
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = vm;
            vm.DisplayNotes.CollectionChanged += DisplayNotes_CollectionChanged;
            vm.NoteChanged += Vm_NoteChanged;
        }

        private void Vm_NoteChanged(object sender, NoteChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() => this.UpdateLayout(), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void DisplayNotes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() => this.UpdateLayout(), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void AddNoteClick(object sender, RoutedEventArgs e)
        {
            vm.CreateNote(this.NoteText.Text);
        }

        private void DeleteNoteClick(object sender, RoutedEventArgs e)
        {
            var s = sender as FrameworkElement;
            if (s.DataContext is NoteViewModel)
            {
                var note = s.DataContext as NoteViewModel;
                vm.DeleteNote(note.Guid);
            }
        }

        private void ConfirmEditNoteClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ConfirmEdit(sender);
        }

        private void ConfirmEdit(object sender)
        {
            var note = GetNoteViewModel(sender);
            if (note is NoteEditModel)
            {
                //vm.UpdateNote(note as NoteEditModel);
                vm.ConfirmEdit(note as NoteEditModel);
            }
        }

        private void EditNoteClick(object sender, RoutedEventArgs e)
        {
            HandleEditNote(sender);
        }

        private INoteViewModel GetNoteViewModel(object sender)
        {
            var s = sender as FrameworkElement;
            if (s.DataContext is INoteViewModel)
            {
                var note = s.DataContext as INoteViewModel;
                return note;
            }
            return null;
        }

        private void HandleEditNote(object sender)
        {
            var note = GetNoteViewModel(sender);
            if (note is NoteViewModel)
            {
                vm.EditNote(note as NoteViewModel);
            }
        }

        private void NoteListLeftButtonClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                HandleEditNote(sender);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var noteSelected = false;
            foreach (INoteViewModel note in e.AddedItems)
            {
                vm.SelectNote(note as NoteModelBase);
                noteSelected = true;
            }
            if (!noteSelected)
            {
                vm.ShowAllNotes();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmEdit(sender);
            }
        }

        private void NoteEditText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var note = GetNoteViewModel(sender);
            // only capture in edit mode
            if (note is NoteEditModel)
            {
                var editNote = note as NoteEditModel;
                var textBox = sender as TextBox;
                var orgText = editNote.Text;
                note.Text = textBox.Text;
                vm.PostTextChanges(note, e.Changes, orgText);
            }
        }

        private async void PlaybackClick(object sender, RoutedEventArgs e)
        {
            var note = GetNoteViewModel(sender);
            if (note is NoteViewModel)
            {
                var noteView = note as NoteModelBase;
                noteView.PropertyChanged += NoteView_PropertyChanged;
                var t = new Task(() => Playback(noteView, sender as Control));
                t.Start();
            }
        }

        private void NoteView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var ctl = sender as Control;
            // ctl.Parent.Dispatcher.Invoke(EmptyDelegate, System.Windows.Threading.DispatcherPriority.Render);
            Debug.WriteLine(sender.ToString());
        }

        private Action EmptyDelegate = delegate () { };
        internal void Playback(NoteModelBase note, Control sender)
        {
            //vm.SelectNote(note);
            note.Text = "";
            var delayCount = 0;
            for (int i = note.Commands.Count - 1; i >= 0; i--)
            {
                delayCount++;
                var command = note.Commands[i];
                PlayCommand(note, sender, command, 250);
            }
        }

        // plays back the command history of a note visually
        private void PlayCommand(INoteViewModel note, Control sender, INoteCommand command, int delayCount)
        {
            Task.Delay(delayCount).Wait();
            Debug.WriteLine(command.ToString());
            if (command is CreateNoteCommand)
            {
                var createCommand = command as CreateNoteCommand;
                var text = createCommand.Text;
                UpdateUI(note, sender, text);
                //note.Text = createCommand.Text;
            }
            else if (command is DeleteTextFromNoteCommand)
            {
                var deleteTextCommand = command as DeleteTextFromNoteCommand;
                var text = note.Text;
                text = text.Remove(deleteTextCommand.Offset, deleteTextCommand.Text.Length);
                UpdateUI(note, sender, text);
            }
            else if (command is InsertTextIntoNoteCommand)
            {
                var insertTextCommand = command as InsertTextIntoNoteCommand;
                var text = note.Text;
                if (insertTextCommand.Offset == note.Text.Length)
                {
                    text += insertTextCommand.Text;
                    UpdateUI(note, sender, text);
                }
                else
                {
                    text = text.Insert(insertTextCommand.Offset, insertTextCommand.Text);
                    UpdateUI(note, sender, text);
                }
            }
        }

        private void UpdateUI(INoteViewModel note, Control sender, string text)
        {
            sender.Parent.Dispatcher.Invoke(() => note.Text = text, System.Windows.Threading.DispatcherPriority.Render);
            Debug.WriteLine(text);
        }

        // plays back a command in the UI creating new commands
        private void RedoCommand(INoteViewModel note, Control sender, INoteCommand command)
        {
            if (command is CreateNoteCommand)
            {
                var createCommand = command as CreateNoteCommand;
                var text = createCommand.Text;
                vm.CreateNote(text);
                //UpdateUI(note, sender, text);
            }
            else if (command is DeleteTextFromNoteCommand)
            {
                var deleteTextCommand = command as DeleteTextFromNoteCommand;
                note.RemoveText(deleteTextCommand.Offset, deleteTextCommand.Text.Length, true);
                //UpdateUI(note, sender, text);
            }
            else if (command is InsertTextIntoNoteCommand)
            {
                var insertTextCommand = command as InsertTextIntoNoteCommand;
                vm.InsertText(note, insertTextCommand.Offset, insertTextCommand.Text, true);
                //UpdateUI(note, sender, note.Text);
            }
            else if (command is DeleteNoteCommand)
            {
                vm.DeleteNote(command.NoteGuid);
                //UpdateUI(note, sender, note.Text);
            }
        }

        private void ReplayAllClick(object sender, RoutedEventArgs e)
        {
            vm.ReplayAllCommands();
        }
    }
}
