namespace FloodFill
{
    public class Cell
    {
        public int X
        {
            get => _x;
            set => _x = value;
        }

        public int Y
        {
            get => _y;
            set => _y = value;
        }

        private int _x;
        private int _y;
        
        public Cell( int x, int y)
        {
        _x = x;
        _y = y;
        }
    }
}