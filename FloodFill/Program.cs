using System;
using System.Collections;

namespace FloodFill
{
    class Program
    {
        private static int[,] map = new int[50, 50];
        private static char[] cellType = {' ', '.', '#'}; //chao,chao pintado,parede

        public static void InicializaMapa()
        {
            Random rnd = new Random();
            for (int k = 0; k < 50; k++)
            {
                int linha = rnd.Next(0, 40);
                int coluna = rnd.Next(0, 40);
                int larg = rnd.Next(0, 9);
                int alt = rnd.Next(0, 9);
                for (int i = linha; i < linha + alt; i++)
                {
                    for (int j = coluna; j < coluna + larg; j++)
                    {
                        map[i, j] = 2;  //parede
                    }
                }
            }
        }

        public static void ShowMap()
        {
            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    Console.Write(cellType[map[i, j]]);
                }

                Console.WriteLine("|");
            }
            Console.WriteLine();
        }

        public static void Fill(int x, int y)
        {
            Boolean walk = true;
            Stack cells = new Stack();
            Cell loc;

            if (map[x, y] == 0)
                cells.Push(new Cell(x, y));
            else
                walk = false;
            
            while (walk)
            {
                if (cells.Count == 0)
                {
                    walk = false;
                }
                else
                {
                    loc = (Cell) cells.Pop();
                    x = loc.X;
                    y = loc.Y;
                    if (map[x, y] == 0) //se for chao pinta
                        map[x, y] = 1;
                    //verifica os 4 vizinhos (se sao chao)
                    if (x > 0)
                    {
                        if (map[x - 1, y] == 0)
                            cells.Push(new Cell(x - 1, y));
                    }

                    if (x < 49)
                    {
                        if (map[x + 1, y] == 0)
                            cells.Push(new Cell(x + 1, y));
                    }

                    if (y > 0)
                    {
                        if (map[x, y - 1] == 0)
                            cells.Push(new Cell(x, y - 1));
                    }

                    if (y < 49)
                    {
                        if (map[x, y + 1] == 0)
                            cells.Push(new Cell(x, y + 1));
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Fill nao recursivo");
            InicializaMapa();
            ShowMap();
            Fill(44,25);
            ShowMap();
        }
    }
}