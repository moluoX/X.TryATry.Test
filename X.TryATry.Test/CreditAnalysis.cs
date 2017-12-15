using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using X.TryATry.Test.Model;
using YCloud.MFBP.Utilities;

namespace X.TryATry.Test
{
    public class CreditAnalysis
    {
        #region 解析征信报告

        public static void Analysis(string path)
        {
            Regex regex;
            Match match;

            //pdf -> string
            var text = ReadPdf(path);
            text = Pretreatment(text);

            //定位，截取
            int headStart = -1, headEnd = -1;
            int head2Start = -1, head2End = -1;
            int personalStart = -1, personalEnd = -1;
            int personal2Start = -1, personal2End = -1;

            regex = new Regex(@"\n+\s*报告编号");
            match = regex.Match(text);
            headStart = match.Index;

            regex = new Regex(@"\n+\s*被查询者姓名[^\n]*\n+");
            match = regex.Match(text);
            headEnd = match.Index;
            head2Start = match.Index + match.Length;

            regex = new Regex(@"\n+[^\n]*个人基本信息[^\n]*\n+");
            match = regex.Match(text);
            head2End = match.Index;

            regex = new Regex(@"\n+\s*性别[^\n]*\n+");
            match = regex.Match(text);
            personalStart = match.Index + match.Length;

            regex = new Regex(@"\n+\s*单位电话[^\n]*\n+");
            match = regex.Match(text);
            personal2Start = match.Index + match.Length;

            regex = new Regex(@"\n+\s*数据发生机构名称[^\n]*\n+");
            match = regex.Match(text);
            personalEnd = match.Index;
            match = match.NextMatch();
            personal2End = match.Index;

            int livingStart = -1, livingEnd = -1;
            regex = new Regex(@"\n+\s*居住信息\s*\n+");
            match = regex.Match(text);
            livingStart = match.Index + match.Length;

            regex = new Regex(@"\n+\s*职业信息\s*\n+");
            match = regex.Match(text);
            livingEnd = match.Index;

            int jobStart = -1, jobEnd = -1;
            regex = new Regex(@"\n+\s*职业信息\s*\n+");
            match = regex.Match(text);
            jobStart = match.Index + match.Length;

            regex = new Regex(@"\n+[\s|二]*信息概要\s*\n+");
            match = regex.Match(text);
            jobEnd = match.Index;

            //AnalysisPersonal(text.Substring(headStart, headEnd - headStart));
            AnalysisLiving(text.Substring(livingStart, livingEnd - livingStart));
            AnalysisJob(text.Substring(jobStart, jobEnd - jobStart));
        }

        private static void AnalysisPersonal(string text)
        {
            {
                var regex = new Regex(@"姓名:*\s*[\u4E00-\u9FA5]+\s");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value.Replace("姓名", "").Replace(":", "").Trim();
                }
            }
            {
                var regex = new Regex(@"证件类型:*\s*[\u4E00-\u9FA5]+\s");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value.Replace("证件类型", "").Replace(":", "").Trim();
                }
            }
            {
                var regex = new Regex(@"证件号码:*\s*\*+[0-9X]+\s");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value.Replace("证件号码", "").Replace(":", "").Replace("*", "").Trim();
                }
            }
            {
                var regex = new Regex(@"已婚|未婚|离异|再婚|丧偶");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value;
                }
            }
            {
                var regex = new Regex(@"报告编号:*\s*\d+");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value.Replace("报告编号", "").Replace(":", "").Trim();
                }
            }
            {
                var regex = new Regex(@"查询时间:*\s*[0-9\.:\-\s]+");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value.Replace("查询时间", "").Replace(":", "").Trim();
                }
            }
            {
                var regex = new Regex(@"报告时间:*\s*[0-9\.:\-\s]+");
                var match = regex.Match(text);
                if (match.Success)
                {
                    var value = match.Value.Replace("报告时间", "").Replace(":", "").Trim();
                }
            }
        }

        private static IList<CREDIT_LIVINGINFO> AnalysisLiving(string textAll)
        {
            var list = new List<CREDIT_LIVINGINFO>();

            int start, end;

            var regex = new Regex(@"信息更新日期\s*\n+");
            var match = regex.Match(textAll);
            start = match.Index + match.Length;

            regex = new Regex(@"\n+[^\n]*数据发生机构名称\s*\n+");
            match = regex.Match(textAll);
            end = match.Index;

            var text = "\n" + textAll.Substring(start, end - start);

            //分割每行，取每行数据
            var rows = SplitRowSerial(text);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var m = new CREDIT_LIVINGINFO();
                list.Add(m);

                //编号
                m.SN = i + 1;
                //信息更新日期
                m.UPDATEDATE = GetUpdateDateFromRow(ref row);
                //居住状况 从行尾往前追溯到空格
                regex = new Regex(@"[^\s]+$");
                match = regex.Match(row);
                m.STATUS = match.Value;
                //居住地址 取剩下的，去掉空格
                m.ADDRESS = regex.Replace(row, "").Replace(" ", "");
            }

            return list;
        }

        private static IList<CREDIT_JOBINFO> AnalysisJob(string textAll)
        {
            var list = new List<CREDIT_JOBINFO>();

            #region 工作单位部分
            int start1, end1;

            var regex = new Regex(@"单位地址\s*\n+");
            var match = regex.Match(textAll);
            start1 = match.Index + match.Length;

            regex = new Regex(@"\n+[^\n]*信息更新日期\s*\n+");
            match = regex.Match(textAll);
            end1 = match.Index;

            var text1 = "\n" + textAll.Substring(start1, end1 - start1);

            //分割每行，取每行数据
            var rows1 = SplitRowSerial(text1);
            var jobCompanyPatterns = GetConfigJobCompanyPatterns();
            for (int i = 0; i < rows1.Count; i++)
            {
                var row = rows1[i];
                var m = new CREDIT_JOBINFO();
                list.Add(m);

                //编号
                m.SN = i + 1;
                //单位地址是否未填
                if (GetIgnoreSymbolFromRow(ref row, PositionInRow.End))
                {
                    m.COMPANYNAME = regex.Replace(row, "").Replace(" ", "").Trim();
                    m.COMPANYADDR = "—";
                    continue;
                }
                //工作单位是否未填
                if (GetIgnoreSymbolFromRow(ref row, PositionInRow.Start))
                {
                    m.COMPANYNAME = IGNORE_SYMBOL;
                    m.COMPANYADDR = regex.Replace(row, "").Replace(" ", "").Trim();
                    continue;
                }
                //工作单位
                bool matched = false;
                foreach (var pattern in jobCompanyPatterns)
                {
                    //先用配置匹配
                    regex = new Regex(pattern);
                    match = regex.Match(row);
                    if (match.Success)
                    {
                        matched = true;
                        m.COMPANYNAME = match.Value.Replace(" ", "").Trim();
                        row = regex.Replace(row, "");
                        break;
                    }
                }
                if (!matched)
                {
                    //未匹配到，从第一个空格分割
                    var spaceIndex = row.IndexOf(" ");
                    if (spaceIndex > 0)
                    {
                        matched = true;
                        m.COMPANYNAME = row.Substring(0, spaceIndex).Trim();
                        row = row.Substring(spaceIndex);
                    }
                }
                if (!matched)
                {
                    //仍未匹配到，本行全部文本都作为工作单位
                    m.COMPANYNAME = row.Replace(" ", "").Trim();
                    row = "";
                }
                //单位地址
                m.COMPANYADDR = row.Replace(" ", "").Trim();
            }
            #endregion

            #region 职业信息部分
            int start2, end2;

            regex = new Regex(@"信息更新日期\s*\n+");
            match = regex.Match(textAll);
            start2 = match.Index + match.Length;

            regex = new Regex(@"\n+[^\n]*数据发生机构名称\s*\n+");
            match = regex.Match(textAll);
            end2 = match.Index;

            var text2 = "\n" + textAll.Substring(start2, end2 - start2);

            //分割每行，取每行数据
            var rows2 = SplitRowSerial(text2);
            for (int i = 0; i < rows2.Count; i++)
            {
                var row = rows2[i];
                var m = list.Count > i ? list[i] : new CREDIT_JOBINFO();
                list.Add(m);

                //编号
                m.SN = i + 1;
                //信息更新日期
                m.UPDATEDATE = GetUpdateDateFromRow(ref row);
                //年份
                m.INTOYEAR = GetYearFromRow(ref row, PositionInRow.End);
                //职称
                m.INTOYEAR = GetUntilSpaceFromRow(ref row, PositionInRow.End);
            }
            #endregion

            return list;
        }

        #endregion

        #region 工具

        private static string ReadPdf(string path)
        {
            PDDocument doc = PDDocument.load(path);
            PDFTextStripper stripper = new PDFTextStripper();
            string text = stripper.getText(doc);
            return text;
        }

        private static string Pretreatment(string text)
        {
            string r = text;
            r = RhPDFTextParser.Default.ParseText(r);

            //去除\r
            var regex = new Regex(@"\r");
            r = regex.Replace(r, "");

            //去除分页
            regex = new Regex(@"[^\n]*页码[^\n]*\n+");
            r = regex.Replace(r, "");
            regex = new Regex(@"\n+[^\n]*http://[^\n]*");
            r = regex.Replace(r, "");

            //错别字
            var erroeWords = GetConfigErroeWords();
            foreach (var x in erroeWords)
            {
                r = r.Replace(x.Key, x.Value);
            }

            return r;
        }

        /// <summary>
        /// 切出行，行号是顺序的，如1 2 3 4 5
        /// </summary>
        /// <returns></returns>
        private static List<string> SplitRowSerial(string text)
        {
            //分割每行
            var splitors = new List<Tuple<int, int>>();
            Regex regex;
            Match match;
            for (int i = 1; ; i++)
            {
                regex = new Regex(@"\n+" + i + @"\s+");
                match = regex.Match(text);
                if (!match.Success)
                    break;
                splitors.Add(new Tuple<int, int>(match.Index, match.Index + match.Length));
            }

            //取每行文本，换行转空格
            var rows = new List<string>();
            for (int i = 0; i < splitors.Count; i++)
            {
                var startThis = splitors[i].Item2;
                var endThis = splitors.Count > i + 1 ? splitors[i + 1].Item1 : text.Length;
                var textRow = text.Substring(startThis, endThis - startThis).Replace("\n", " ").Trim();
                rows.Add(textRow);
            }
            return rows;
        }

        /// <summary>
        /// 从行文本中截取信息更新日期，格式 2014. 02. 27
        /// </summary>
        /// <returns></returns>
        private static DateTime? GetUpdateDateFromRow(ref string row)
        {
            var regex = new Regex(@"\d{4}\.\s*\d{2}\.\s*\d{2}");
            var match = regex.Match(row);
            DateTime? r = null;
            if (match.Success)
            {
                r = match.Value.XToDateTimeOrNull();
                row = regex.Replace(row, "").Trim();
            }
            return r;
        }

        /// <summary>
        /// 从行文本中截取—
        /// </summary>
        /// <returns></returns>
        private static bool GetIgnoreSymbolFromRow(ref string row, PositionInRow position)
        {
            Regex regex;
            switch (position)
            {
                case PositionInRow.Start:
                    regex = new Regex(@"^" + IGNORE_SYMBOL + @"\s");
                    break;
                case PositionInRow.End:
                    regex = new Regex(@"\s" + IGNORE_SYMBOL + @"$");
                    break;
                default:
                    regex = new Regex(@"\s" + IGNORE_SYMBOL + @"\s+");
                    break;
            }
            var match = regex.Match(row);
            bool r = false;
            if (match.Success)
            {
                r = true;
                row = regex.Replace(row, "").Trim();
            }
            return r;
        }

        /// <summary>
        /// 从行文本头或尾开始截取文本直到空白符
        /// </summary>
        /// <param name="row"></param>
        /// <param name="position"></param>
        /// <param name="checkIngnore">是否先从行文本中截取—</param>
        /// <returns></returns>
        private static string GetUntilSpaceFromRow(ref string row, PositionInRow position, bool checkIngnore = true)
        {
            if (checkIngnore && GetIgnoreSymbolFromRow(ref row, position))
                return IGNORE_SYMBOL;

            Regex regex;
            switch (position)
            {
                case PositionInRow.Start:
                    regex = new Regex(@"^\S+\s");
                    break;
                case PositionInRow.End:
                    regex = new Regex(@"\s\S+$");
                    break;
                default:
                    throw new NotImplementedException("GetUntilSpaceFromRow不支持此position");
            }
            var match = regex.Match(row);
            string r = "";
            if (match.Success)
            {
                r = match.Value.Trim();
                row = regex.Replace(row, "").Trim();
            }
            return r;
        }

        /// <summary>
        /// 从行文本头或尾开始截取年
        /// </summary>
        /// <param name="row"></param>
        /// <param name="position"></param>
        /// <param name="checkIngnore">是否先从行文本中截取—</param>
        /// <returns></returns>
        private static string GetYearFromRow(ref string row, PositionInRow position, bool checkIngnore = true)
        {
            if (checkIngnore && GetIgnoreSymbolFromRow(ref row, position))
                return IGNORE_SYMBOL;

            Regex regex;
            switch (position)
            {
                case PositionInRow.Start:
                    regex = new Regex(@"^[12]\d{3}");
                    break;
                case PositionInRow.End:
                    regex = new Regex(@"[12]\d{3}$");
                    break;
                default:
                    throw new NotImplementedException("GetYearFromRow不支持此position");
            }
            var match = regex.Match(row);
            string r = "";
            if (match.Success)
            {
                r = match.Value.Trim();
                row = regex.Replace(row, "").Trim();
            }
            return r;
        }

        #endregion

        #region 配置

        private const string ERROR_WORD = @"万M行=万里行
深训=深圳";
        /// <summary>
        /// 配置：错别字
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> GetConfigErroeWords()
        {
            var r = new Dictionary<string, string>();
            var list = ERROR_WORD.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var x in list)
            {
                if (string.IsNullOrWhiteSpace(x) || x.IndexOf('=') <= 0)
                    continue;
                var a = x.Split('=');
                if (a.Length < 2)
                    continue;
                if (string.IsNullOrWhiteSpace(a[0]))
                    continue;
                if (r.ContainsKey(a[0]))
                    r[a[0]] = a[1];
                else
                    r.Add(a[0], a[1]);
            }
            return r;
        }

        private const string JOB_COMPANYNAME_PATTERN = @"^.+公司\s+
^.+部\s+
^.+店\s+";
        /// <summary>
        /// 配置：工作单位正则表达式
        /// </summary>
        /// <returns></returns>
        private static string[] GetConfigJobCompanyPatterns()
        {
            return JOB_COMPANYNAME_PATTERN.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private const string IGNORE_SYMBOL = "—";

        #endregion
    }

    enum PositionInRow
    {
        Start = 0,
        Middle = 1,
        End = 2,
    }
}
