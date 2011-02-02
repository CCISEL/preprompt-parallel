namespace FindInFiles
{
    public class Match
    {
        public string File;
        public int Line;
        public string Text;

        public override string ToString()
        {
            return string.Format("{0}:{1} {2}", File, Line, Text);
        }
    }
}