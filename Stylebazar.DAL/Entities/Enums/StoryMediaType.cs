namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// What kind of media a Story carries. Drives both the storage flow (image vs.
/// video file-type validation) and the viewer rendering (img vs. video element).
/// </summary>
public enum StoryMediaType
{
    Image = 0,
    Video = 1
}
