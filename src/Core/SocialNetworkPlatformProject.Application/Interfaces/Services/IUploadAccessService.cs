namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Decides who may open an uploaded file. The file is found by its address among the things that hold one (a post's photo, a
// message, a story, a story kept in a highlight, an event's cover), and the viewer needs the same right to that thing as to open it in the pages:
// a friends-only post's photo is for its author's friends, a chat photo for the two people of the chat, a story for the
// friends of its author while it lasts. Pictures that every signed-in person sees on the pages anyway (profile photos,
// group covers, marketplace photos) are open to any signed-in person.
public interface IUploadAccessService
{
    // folder is the first part of the address after /uploads/ ("posts", "messages"...), url the whole address ("/uploads/posts/x.jpg").
    Task<bool> CanViewAsync(Guid viewerId, string folder, string url);
}
