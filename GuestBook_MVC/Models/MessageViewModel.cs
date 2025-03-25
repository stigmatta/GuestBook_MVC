using GuestBook_MVC.Models;

public class MessageViewModel
{
    public string? MessageText { get; set; }
    public ICollection<Message> Messages { get; set; } = null!;
}
