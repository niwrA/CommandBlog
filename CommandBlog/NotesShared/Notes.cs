using System;
using System.Collections.Generic;
using System.Text;

namespace NotesShared
{
    public interface INoteState
    {
        Guid Guid { get; set; }
        string Text { get; set; }
    }

    public interface INoteRepository
    {
        INoteState CreateNoteState();

        void UpdateNote(INoteState state);
        void DeleteNote(Guid guid);
    }

    public interface INote
    {
        Guid Guid { get; }
        string Text { get; }
    }
    public class Note : INote
    {
        INoteState _state;
        INoteRepository _repo;
        public Note(INoteRepository repo)
        {
            _repo = repo;
            if (_state == null)
            {
                _state = repo.CreateNoteState();
                _state.Guid = Guid.NewGuid();
            }
        }

        public Note(INoteRepository repo, INoteState state) : this(repo)
        {
            _repo = repo;
            _state = state;
        }

        public void UpdateText(string text)
        {
            _state.Text = text;
        }

        public Guid Guid { get { return _state.Guid; } }
        public string Text { get { return _state.Text; } }
    }

    public class Notes
    {
        INoteRepository _repo;
        public Notes(INoteRepository repo)
        {
            _repo = repo;
        }

        public INote Create(string text)
        {
            var note = new Note(_repo);
            note.UpdateText(text);
            return note;
        }

        public void Delete(Guid guid)
        {
            _repo.DeleteNote(guid);
        }
    }
}
