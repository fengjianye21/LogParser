using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Parser
{
    class Cost : ICollection<uint>
    {
        public Cost()
        {
            costs_ = new List<uint>();
        }

        // ICollection<> interfaces
        public int Count
        {
            get
            {
                return costs_.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException("Cost.IsReadOnly not implemented.");
            }
        }

        public void Add(uint c)
        {
            costs_.Add(c);
        }

        public void Clear()
        {
            costs_.Clear();
        }

        public bool Contains(uint c)
        {
            return costs_.Contains(c);
        }

        public void CopyTo(uint[] array, int arrayIndex)
        {
            costs_.CopyTo(array, arrayIndex);
        }

        public bool Remove(uint c)
        {
            return costs_.Remove(c);
        }

        // IEnumerable<> interface
        public IEnumerator<uint> GetEnumerator()
        {
            return costs_.GetEnumerator();
        }

        // IEnumerable interface
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return costs_.GetEnumerator();
        }

        private List<uint> costs_;
    }

    class Para2Cost : ICollection<KeyValuePair<string, Cost>>
    {
        public Para2Cost()
        {
            para2Cost_ = new Dictionary<string, Cost>();
        }

        /// <summary>
        /// 性能测试结果中，某个具体参数对应的时间值
        /// </summary>
        /// <param name="strPara">性能测试中某个统计函数的参数值</param>
        /// <param name="c">改参数对应的客户端处理时间</param>
        public void Add(string strPara, uint c)
        {
            if (!para2Cost_.ContainsKey(strPara))
            {
                para2Cost_.Add(strPara, new Cost());
            }

            para2Cost_[strPara].Add(c);
        }

        /// <summary>
        /// 根据函数参数数据值找到对应的Cost记录
        /// </summary>
        /// <param name="param">性能测试中某个统计函数的参数值</param>
        /// <returns></returns>
        public Cost GetCostBy(string param)
        {
            return para2Cost_[param];
        }

        // ICollection<> interfaces
        public int Count
        {
            get
            {
                return para2Cost_.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException("Para2Cost.IsReadyOnly not implemented.");
            }
        }

        public void Add(KeyValuePair<string, Cost> value)
        {
            para2Cost_.Add(value.Key, value.Value);
        }

        public void Clear()
        {
            para2Cost_.Clear();
        }

        public bool Contains(KeyValuePair<string, Cost> val)
        {
            throw new NotImplementedException("Para2Cost.Contains not implemented.");
        }

        public void CopyTo(KeyValuePair<string, Cost>[] array, int arrayIndex)
        {
            throw new NotImplementedException("Para2Cost.CopyTo not implemented.");
        }

        public bool Remove(KeyValuePair<string, Cost> val)
        {
            throw new NotImplementedException("Para2Cost.Remove not implemented.");
        }

        // IEnumerable<> interface
        public IEnumerator<KeyValuePair<string, Cost>> GetEnumerator()
        {
            return para2Cost_.GetEnumerator();
        }

        // IEnumerable interface
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException("Para2Cost.IEnumerable.GetEnumerator not implemented.");
        }


        private Dictionary<string, Cost> para2Cost_;
    }

    class LogParser
    {
        public LogParser(string logPath, string resultPath)
        {
            logPath_ = logPath;
            resultPath_ = resultPath;

            if (!File.Exists(logPath_))
            {
                throw new FileNotFoundException("The performance log file not found.");
            }

            results_ = new Dictionary<string, Para2Cost>();
        }

        /// <summary>
        /// 解析日志文件并生成结果文件
        /// </summary>
        public void Process()
        {
            Parse(logPath_);
            WriteResult(resultPath_);
        }

        /// <summary>
        /// 解析日志文件
        /// </summary>
        /// <param name="strPath">日志文件的路径</param>
        protected void Parse(string strPath)
        {
            String strRecords = null;
            using (StreamReader sr = new StreamReader(strPath))
            {
                strRecords = sr.ReadToEnd();
            }

            using (StringReader strReader = new StringReader(strRecords))
            {
                // 使用stack来匹配每一行的记录，当缩进一致的时候，就是一条完整的记录。
                Stack<string> strStack = new Stack<string>();
                while (strReader.Peek() >= 0)
                {
                    string strLine = strReader.ReadLine();
                    if (0 == strStack.Count)
                    {
                        strStack.Push(strLine);
                    }
                    else
                    {
                        string strTop = strStack.Peek();

                        int idx = 0;
                        for (; idx < Math.Min(strTop.Length, strLine.Length); ++idx)
                        {
                            char ch1 = strTop[idx];
                            char ch2 = strLine[idx];

                            if (' ' != ch1 || ' ' != ch2)
                            {
                                break;
                            }
                        }

                        // 到这里，至少有一个不是空格了
                        Debug.Assert(' ' != strLine[idx] || ' ' != strTop[idx]);

                        if (' ' == strTop[idx] || ' ' == strLine[idx])
                        {
                            strStack.Push(strLine);
                        }
                        else
                        {
                            strStack.Pop();

                            ParseRecord(strTop, strLine);
                        }
                    }
                }

                Debug.Assert(strStack.Count == 0);
            }
        }

        /// <summary>
        /// 解析一条完整的记录
        /// </summary>
        /// <param name="begin">一条完整记录的起始行</param>
        /// <param name="end">一条完整记录的终止行</param>
        protected void ParseRecord(string begin, string end)
        {
            // CMainFrame.OnCreate:[35996247]
            // Page.DataReady:[65535.36866.35997371]
            // Obj.ResetSize: [35997339]
            Regex beginRegex = new Regex(@"(\S+)\s*:\s*\[(.+)\]");
            //      [35997417]
            Regex endRegex = new Regex(@"\s*\[(\d+)\]");

            MatchCollection matchBegin = beginRegex.Matches(begin);
            MatchCollection matchEnd = endRegex.Matches(end);

            Debug.Assert(1 == matchBegin.Count);
            Debug.Assert(1 == matchEnd.Count);

            GroupCollection beginGroups = matchBegin[0].Groups;
            GroupCollection endGroups = matchEnd[0].Groups;

            Debug.Assert(3 == beginGroups.Count);
            Debug.Assert(2 == endGroups.Count);

            uint tickcntEnd = Convert.ToUInt32(endGroups[1].Value);
            uint tickcntBegin = 0;
            string paraAndCost = beginGroups[2].Value;
            int idx = paraAndCost.LastIndexOf('.');
            tickcntBegin = Convert.ToUInt32(paraAndCost.Substring(++idx));

            if (!results_.ContainsKey(beginGroups[1].Value))
            {
                results_.Add(beginGroups[1].Value, new Para2Cost());
            }
            results_[beginGroups[1].Value].Add(0 == idx ? "default" : paraAndCost.Substring(0, idx), tickcntEnd - tickcntBegin);
        }

        /// <summary>
        /// 将Parse统计出来的结果写盘
        /// </summary>
        /// <param name="resultPath">结果文件的路径</param>
        protected void WriteResult(string resultPath)
        {
            // 创建一个Excel应用程序以及Workbook和Worksheet。
            MSExcel.Application excel = new MSExcel.Application();

            if (excel.Workbooks.Count <= 0)
            {
                excel.Workbooks.Add();
            }
            MSExcel.Workbook excelBook = excel.Workbooks[1];    // workbook index starts from 1.

            if (excelBook.Worksheets.Count <= 0)
            {
                excelBook.Worksheets.Add();
            }
            MSExcel.Worksheet excelSheet = excelBook.Worksheets[1] as MSExcel.Worksheet;    // worksheet index starts from 1.

            // 将数据填充进单元格
            // 数据量大的话，效率是个问题
            int row = 1;
            foreach (var pc in results_)
            {
                excelSheet.Cells[row, 1] = pc.Key;    // 第一列是results_的Key，函数的名称
                foreach (var record in pc.Value)
                {
                    excelSheet.Cells[row, 2] = record.Key;  // 第二列是函数的参数

                    foreach (var cost in record.Value)
                    {
                        excelSheet.Cells[row++, 3] = cost;  // 第三列是时间
                    }
                }
            }

            if (File.Exists(resultPath))
            {
                File.Delete(resultPath);
            }

            excelBook.SaveAs(resultPath);
            excelBook.Close();
            excel.Quit();
        }

        private string logPath_;
        private string resultPath_;
        private Dictionary<string, Para2Cost> results_;
    }
}
