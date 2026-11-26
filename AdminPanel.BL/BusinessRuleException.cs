namespace AdminPanel.BL;

// ЗАВДАННЯ vi (валідація): порушення бізнес-правила.
// Errors — список зрозумілих користувачу повідомлень, ViewModel показує їх у вікні
public class BusinessRuleException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public BusinessRuleException(string error) : this([error])
    {
    }

    public BusinessRuleException(IReadOnlyList<string> errors) : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors;
    }
}