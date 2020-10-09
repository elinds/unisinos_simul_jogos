/*
 * CA engine 0.2
 */
namespace CAengine
{
    public class Cell
    {
        public Cell(int lin, int col, int hei, int value)
        {
            _lin = lin;
            _col = col;
            _hei = hei;
            _value = value;
        }

        public int Lin
        {
            get => _lin;
            set => _lin = value;
        }

        public int Col
        {
            get => _col;
            set => _col = value;
        }

        public int Hei
        {
            get => _hei;
            set => _hei = value;
        }

        public int Value
        {
            get => _value;
            set => _value = value;
        }

        private int _lin;
        private int _col;
        private int _hei;
        private int _value;
    }
}