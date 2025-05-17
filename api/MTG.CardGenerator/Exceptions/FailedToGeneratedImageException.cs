namespace MTG.CardGenerator.Exceptions
{
    public class FailedToGenerateImageException : Exception
    {
        public string Prompt { get; }
        public string Model { get; }

        public FailedToGenerateImageException(string message, string prompt, string model) : base(message)
        {
            Prompt = prompt;
            Model = model;
        }

        public FailedToGenerateImageException(string message, string prompt, string model, Exception innerException) : base(message, innerException)
        {
            Prompt = prompt;
            Model = model;
        }
    }
}
