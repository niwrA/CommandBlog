using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MomentSharp;

namespace NoteCommandFactoryShared
{
    public interface INoteCommand
    {
        Guid NoteGuid { get; set; }
        DateTime CreatedOn { get; }
        string ToString();
    }
    public abstract class NoteCommandBase : INoteCommand
    {
        public Guid NoteGuid { get; set; }
        public DateTime CreatedOn { get; } = DateTime.UtcNow;
    }
    public class CreateNoteCommand : NoteCommandBase
    {
        public string Text { get; set; }
        public override string ToString()
        {
            return $"{Parse.Moment(CreatedOn).From()}\nCreate: {Text}";
        }
    }
    public class UpdateNoteCommand : NoteCommandBase
    {
        public string Text { get; set; }
        public override string ToString()
        {
            return $"{Parse.Moment(CreatedOn).From()}\nUpdate: {Text}";
        }
    }

    public class InsertTextIntoNoteCommand : NoteCommandBase
    {
        public int Offset { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            return $"{Parse.Moment(CreatedOn).From()}\nInsertText: {Offset}:{Text}";
        }
    }

    public class DeleteTextFromNoteCommand : NoteCommandBase
    {
        public int Offset { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            return $"{Parse.Moment(CreatedOn).From()}\nDeleteText: {Offset}:{Text}";
        }
    }


    public class DeleteNoteCommand : NoteCommandBase
    {
        public override string ToString()
        {
            return $"{Parse.Moment(CreatedOn).From()}\nDelete: {NoteGuid}";
        }
    }

    public class NoteCommandsController : INoteCommandsController
    {
        public INoteCommand Create(Guid guid, string text)
        {
            var command = new CreateNoteCommand { NoteGuid = guid, Text = text };
            return command;
        }

        public INoteCommand Delete(Guid guid)
        {
            var command = new DeleteNoteCommand { NoteGuid = guid };
            return command;
        }

        public INoteCommand DeleteText(Guid guid, int offset, string text)
        {
            var command = new DeleteTextFromNoteCommand { NoteGuid = guid, Text = text, Offset = offset};
            return command;
        }

        public INoteCommand InsertText(Guid guid, int offset, string text)
        {
            var command = new InsertTextIntoNoteCommand { NoteGuid = guid, Text = text, Offset = offset };
            return command;
        }

        public INoteCommand Update(Guid guid, string text)
        {
            var command = new UpdateNoteCommand { NoteGuid = guid, Text = text };
            return command;
        }
    }
}
