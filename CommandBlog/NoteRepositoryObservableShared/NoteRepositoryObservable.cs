using NotesShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NoteRepositoryObservableShared
{
    public class NoteRepositoryObservable //: INoteRepository
    {
        public INoteState CreateNoteState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void DeleteNote(Guid guid)
        {
            throw new NotImplementedException();
        }

        public void UpdateNote(INoteState state)
        {
            throw new NotImplementedException();
        }
    }
}
