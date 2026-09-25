namespace SocialNetworkPlatformProject.Domain.Enums;

// Text is a normal bubble, Image carries MediaUrl (+ optional caption in Text), Sticker is a single large emoji in Text.
public enum MessageType
{
    Text = 0,
    Image = 1,
    Sticker = 2
}
