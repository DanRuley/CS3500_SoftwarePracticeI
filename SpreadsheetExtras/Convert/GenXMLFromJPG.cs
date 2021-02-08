using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS;
using CVTR;

namespace Convert
{
    public class GenXMLFromJPG
    {

        private Converter _converter;

        private int _cell_length;

        private Spreadsheet _spread_image;

        private string _ascii;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file">JPG/PNG to be opened</param>
        /// <param name="sprd">Spreadsheet to hold the contents.</param>
        public GenXMLFromJPG(string file, int len, Func<int, bool> verify_len, int cell_l)
        {
            if (!verify_len(len)) throw new Exception("Invalid Length");

            this._cell_length = cell_l;
            this._converter = new Converter(file, len);
            this._ascii = _converter.GenerateAscii();
            this._spread_image = new Spreadsheet();
        }

        public GenXMLFromJPG(string file, Spreadsheet sprd, int len, Func<int, bool> verify_len, int cell_l)
        {
            if (!verify_len(len)) throw new Exception("Invalid Length");
            this._cell_length = cell_l;
            this._converter = new Converter(file, len);
            this._ascii = _converter.GenerateAscii();
            this._spread_image = sprd;
        }

        public GenXMLFromJPG(string ascii_string, Spreadsheet sprd, int cell_l)
        {
            this._cell_length = cell_l;
            this._ascii =ascii_string;
            this._spread_image = sprd;
        }

        public Spreadsheet GenFilledSpread()
        {
            string[] lines = this._ascii.Split( new[] { Environment.NewLine },StringSplitOptions.None);
            int num = 0;
            foreach (string l in lines)
            {
                num++;
                char col = 'A';
                string cont = "";
                for (int i = 0; i < l.Length; i++)
                {
                    cont = cont + l[i];
                    if ((i + 1) % _cell_length == 0)
                    {
                        try
                        {
                            _spread_image.SetContentsOfCell(col.ToString() + num, cont);
                        }
                        catch (Exception)
                        {
                            if (num >= 100) break;
                            else continue;
                        }
                        finally
                        {
                            cont = "";
                            col = (char)(col + 1);
                        }
                    }
                }
            }
            return this._spread_image;
        }
    }
}
