using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSS
{
    public class QueryStringBuilder
    {
        public string Sql
        {
            set { }
            get
            {
                Select = Select.Trim();
                if (Select.IndexOf(';') == Select.Length - 1) Select = Select.Substring(0, Select.Length - 1);

                return
                     string.Concat(
                         new string[]
                         {
                                 Select,
                                 "\r\n",
                                 From,
                                 "\r\n",
                                 Where,
                                 "\r\n",
                                 OrderBy
                         });
            }
        }
        public string Select { set; get; }
        public string From { set; get; }
        public string Where { set; get; }
        public string OrderBy { set; get; }

        public QueryStringBuilder()
        {
        }
        public QueryStringBuilder(string Select, string From, string Where)
        {
            this.Select = Select;
            this.From = From;
            this.Where = Where;
        }

        public string InitWithFileScriptSQL(string pathFileScriptSQL)
        {
            string msg = ReadFileScriptSQL(pathFileScriptSQL, out string strSQL);
            if (msg.Length > 0) return msg;

            return InitWithStringSQL(strSQL);
        }
        public string InitWithStringSQL(string strSQL)
        {
            string strSelect, strFrom, strWhere, strOrder;
            string msg = GetClausesSQL(strSQL, out strSelect, out strFrom, out strWhere, out strOrder);
            if (msg.Length > 0) return msg;

            Select = strSelect;
            From = strFrom;
            Where = strWhere;
            OrderBy = strOrder;

            return msg;
        }
        public static string ReadFileScriptSQL(string pathFileScriptSQL, out string strSQL)
        {
            strSQL = "";

            if (pathFileScriptSQL.IndexOf('/') == 0 || pathFileScriptSQL.IndexOf('\\') == 0) pathFileScriptSQL = pathFileScriptSQL.Substring(1);
            string fullPath = Path.Combine(AppContext.BaseDirectory, pathFileScriptSQL);

            try
            {
                strSQL = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return "";
        }

        public void AddStringAndWhere(string str)
        {
            if (str.Length > 0)
            {
                if (string.IsNullOrEmpty(Where)) Where = "WHERE ";
                else Where += " AND ";
                Where += str;
            }
        }
        public void AddStringOrderBy(string str)
        {
            if (str.Length > 0)
            {
                if (string.IsNullOrEmpty(OrderBy)) OrderBy = "Order by ";
                else OrderBy += " , ";

                OrderBy += str;
            }
        }

        public static string GetClausesSQL(string strSql, out string strSelect, out string strFrom, out string strWhere, out string strOrderBy)
        {
            strSelect = strFrom = strWhere = strOrderBy = "";

            IList<ParseError> errors;
            var parser = new TSql100Parser(true);
            var script = parser.Parse(new StringReader(strSql), out errors) as TSqlScript;
            if (errors.Count > 0) return string.Join(Environment.NewLine, errors.Select(v => "Dòng " + v.Line + ", cột " + v.Column + ": " + v.Message));

            SelectStatement selectStatement;
            string msg = GetStatement(script, out selectStatement);
            if (msg.Length > 0) return msg;
            if (selectStatement == null) return "Hệ thống không tìm được câu lệnh SELECT";

            return GetClausesSQL(selectStatement, out strSelect, out strFrom, out strWhere, out strOrderBy);
        }
        private static string GetStatement(TSqlScript script, out SelectStatement selectStatement)
        {
            selectStatement = null;

            if (script == null) return "";

            foreach (TSqlBatch batch in script.Batches)
                foreach (TSqlStatement statement in batch.Statements)
                    if (statement is SelectStatement) selectStatement = statement as SelectStatement;

            return "";
        }
        private static string GetClausesSQL(SelectStatement statement, out string strSelect, out string strFrom, out string strWhere, out string strOrderBy)
        {
            strSelect = strFrom = strWhere = strOrderBy = "";

            QueryExpression select = ((QuerySpecification)((SelectStatement)statement).QueryExpression);
            FromClause from = ((QuerySpecification)((SelectStatement)statement).QueryExpression).FromClause;

            if (select != null && from != null)
            {
                for (int i = select.FirstTokenIndex; i < from.FirstTokenIndex; i++)
                    strSelect += select.ScriptTokenStream[i].Text;

                for (int i = from.FirstTokenIndex; i <= from.LastTokenIndex; i++)
                    strFrom += from.ScriptTokenStream[i].Text;
            }

            WhereClause where = ((QuerySpecification)((SelectStatement)statement).QueryExpression).WhereClause;
            if (where != null)
                for (int i = where.FirstTokenIndex; i <= where.LastTokenIndex; i++)
                    strWhere += where.ScriptTokenStream[i].Text;

            OrderByClause orderBy = ((QuerySpecification)((SelectStatement)statement).QueryExpression).OrderByClause;
            if (orderBy != null)
                for (int i = orderBy.FirstTokenIndex; i <= orderBy.LastTokenIndex; i++)
                    strOrderBy += orderBy.ScriptTokenStream[i].Text;

            return "";
        }
    }
}