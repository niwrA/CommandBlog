using System;
using System.Collections.ObjectModel;

namespace NoteCommandFactoryShared
{
    public interface INoteCommandsController
    {
        INoteCommand Create(Guid guid, string text);
        INoteCommand Delete(Guid guid);
        INoteCommand Update(Guid guid, string text);
        INoteCommand DeleteText(Guid guid, int offset, string text);
        INoteCommand InsertText(Guid guid, int offset, string text);
    }
}