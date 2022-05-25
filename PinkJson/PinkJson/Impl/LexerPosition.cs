namespace PinkJson.Impl
{
    public class LexerPosition
    {
        public int CurrentPosition { get; set; }
        public int StartPosition { get; set; }
        public LexerPosition(int current, int start)
        {
            CurrentPosition = current;
            StartPosition = start;
        }
    }
}
