using NotesShared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NoteRepositoryShared
{
    public class NoteRepository : INoteRepository
    {
        public INoteState CreateNoteState()
        {
            throw new NotImplementedException();
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
