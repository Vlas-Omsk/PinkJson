namespace PinkJson.Impl
{
    public class ElementPosition
    {
        public int Start { get; set; }
        public int End { get; set; }
        public ElementPosition(int start, int end)
        {
            End = end;
            Start = start;
        }
        public ElementPosition(int start)
        {
            End = start;
            Start = start;
        }
        public override string ToString()
        {
            return $"({Start}{(End == -1 ? "" : ", " + End)})";
        }
    }
}
