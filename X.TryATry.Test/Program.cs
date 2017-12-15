using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X.TryATry.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            CreditAnalysis.Analysis(@"E:\doc\7-预授信\征信报告\详-李娟玉11.pdf");
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            sw.Restart();
            //ElementReaderAdv.Do(@"E:\doc\7-预授信\征信报告\详-李娟玉11.pdf");
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            //PdfAnalysis.AnalysisCreditSimple(@"E:\doc\7-预授信\征信报告\详版征信old-郑立德-431126198411287058-008.pdf");

            Console.ReadLine();
        }
    }
}
