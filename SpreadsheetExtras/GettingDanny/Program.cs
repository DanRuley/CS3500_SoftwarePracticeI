using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS;

namespace Convert
{
    class GenXMLFromJPG
    {

        static Spreadsheet danny = new Spreadsheet();

        static void read(string filename)
        {
            string[] lines = System.IO.File.ReadAllLines(filename);
            int num = 0;

            foreach (string l in lines)
            {
                num++;
                char col = 'A';
                string cont = "";
                for (int i = 0; i < l.Length; i++)
                {
                    cont = cont + l[i];
                    if((i+1) % 11 == 0)
                    {
                        danny.SetContentsOfCell( col.ToString() + num, cont);
                        cont = "";
                        col = (char)(col+ 1);
                    }
                }
            }
            danny.Save("Danny.xml");
        }

        static void Main(string[] args)
        {
            string filename = "danny_ascii99.txt";
            read(filename);
        }
    }
}
