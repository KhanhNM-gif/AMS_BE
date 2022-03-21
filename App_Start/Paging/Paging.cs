using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using BSS;

namespace BSS
{
    public class Paging
    {
        /// <summary>
        /// Thực hiện Phân trang bằng Stored Proceduced
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="storeName">Tên Stored Proceduced</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="lt">Danh sách đối tượng đã phân trang</param>
        /// <param name="total">Tổng số bản ghi</param>
        /// <returns></returns>
        public static string ExecByStore<T, TL>(string storeName, string columnObjectPagingID, T parameters, out List<TL> lt, out int total) where T : class where TL : class
        {
            lt = null; total = 0;

            string msg = ExecByStore<T>(storeName, columnObjectPagingID, parameters, out DataTable dt, out total);
            if (msg.Length > 0) return msg;

            return Convertor.DataTableToList(dt, out lt);
        }
        /// <summary>
        /// Thực hiện Phân trang bằng Stored Proceduced
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="storeName">Tên Stored Proceduced</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="dt">Bảng dữ liệu đã phân trang</param>
        /// <param name="total">Tổng số bản ghi</param>
        /// <returns></returns>
        public static string ExecByStore<T>(string storeName, string columnObjectPagingID, T parameters, out DataTable dt, out int total) where T : class
        {
            dt = null; total = 0;

            string msg = DBM.ExecStore("sys.sp_helptext", new { objname = storeName }, out DataTable dtText);
            if (msg.Length > 0) return msg;
            if (dtText == null || dtText.Rows.Count == 0) return "Nội dung store " + storeName + " trống";

            int indexBegin = 0, indexEnd = 0;
            for (int i = 0; i < dtText.Rows.Count; i++)
            {
                string text = dtText.Rows[i]["Text"].ToString().Trim().ToLower();
                if (text == "begin") { indexBegin = i; break; }
            }

            for (int i = dtText.Rows.Count - 1; i >= 0; i--)
            {
                string text = dtText.Rows[i]["Text"].ToString().Trim().ToLower();
                if (text == "end" || text == "end;") { indexEnd = i; break; }
            }

            string textSQL = "";
            for (int i = indexBegin + 1; i < indexEnd; i++) textSQL += dtText.Rows[i]["Text"].ToString();

            return Exec<T>(textSQL, columnObjectPagingID, parameters, out dt, out total);
        }

        /// <summary>
        /// Thực hiện Phân trang (đọc file Script SQL)
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="pathFileScriptSQL">Đường dẫn file SQL</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="lt">Danh sách dữ liệu trả về</param>
        /// <param name="total">Tổng số bản ghi</param>
        /// <returns></returns>
        public static string ExecByFileScriptSQL<T, TL>(string pathFileScriptSQL, string columnObjectPagingID, T parameters, out List<TL> lt, out int total) where T : class where TL : class
        {
            lt = null;

            string msg = ExecByFileScriptSQL<T>(pathFileScriptSQL, columnObjectPagingID, parameters, out DataTable dt, out total);
            if (msg.Length > 0) return msg;

            return Convertor.DataTableToList(dt, out lt);
        }

        /// <summary>
        /// Thực hiện Phân trang (đọc file Script SQL)
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="pathFileScriptSQL">Đường dẫn file SQL</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="dt">Bảng dữ liệu đã phân trang</param>
        /// <param name="total">Tổng số bản ghi</param>
        /// <returns></returns>
        public static string ExecByFileScriptSQL<T>(string pathFileScriptSQL, string columnObjectPagingID, T parameters, out DataTable dt, out int total) where T : class
        {
            dt = null; total = 0;

            string msg = QueryStringBuilder.ReadFileScriptSQL(pathFileScriptSQL, out string sqlPaging);
            if (msg.Length > 0) return msg;

            return Exec(sqlPaging, columnObjectPagingID, parameters, out dt, out total);
        }

        /// <summary>
        /// Thực hiện Phân trang theo nội dung file SQL
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="sqlInput">Câu lệnh SQL</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="result">Kết quả trả về</param>
        /// <returns></returns>
        public static string Exec<T>(string sqlInput, string columnObjectPagingID, T parameters, out Result result) where T : class
        {
            result = null;

            string msg = Exec<T>(sqlInput, columnObjectPagingID, parameters, out DataTable dt, out int total);
            if (msg.Length > 0) result = Log.ProcessError(msg).ToResultError();
            else result = (new { Data = dt, Total = total }).ToResultOk();

            return "";
        }
        /// <summary>
        /// Thực hiện Phân trang 
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="sqlInput">Câu lệnh SQL</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="lt">Danh sách trả về</param>
        /// <param name="total">Tổng số đối tượng</param>
        /// <returns></returns>
        public static string Exec<T, TL>(string sqlInput, string columnObjectPagingID, T parameters, out List<TL> lt, out int total) where T : class where TL : class
        {
            lt = null;

            string msg = Exec<T>(sqlInput, columnObjectPagingID, parameters, out DataTable dt, out total);
            if (msg.Length > 0) return msg;

            return Convertor.DataTableToList(dt, out lt);
        }
        /// <summary>
        /// Thực hiện Phân trang 
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu tham số đầu vào</typeparam>
        /// <param name="sqlInput">Câu lệnh SQL</param>
        /// <param name="columnObjectPagingID">Tên cột ID của đối tượng phân trang</param>
        /// <param name="parameters">Các tham số đầu vào. Các tham số bắt buộc phải có: PageSize - Kích thước trang, CurrentPage - Trang hiện tại </param>
        /// <param name="dt">Bảng dữ liệu đã phân trang</param>
        /// <param name="total">Tổng số đối tượng</param>
        /// <returns></returns>
        public static string Exec<T>(string sqlInput, string columnObjectPagingID, T parameters, out DataTable dt, out int total) where T : class
        {
            dt = null; total = 0;

            string msg = ValidateData<T>(sqlInput, columnObjectPagingID, parameters);
            if (msg.Length > 0) return msg;

            msg = GetSql(sqlInput, columnObjectPagingID, out string sqlSelectColumnObjectPagingID, out string sqlSelectResult, out string sqlSelectTotal);
            if (msg.Length > 0) return msg;

            string strSql = @"CREATE TABLE #temp
					        (ID BIGINT IDENTITY(1,1), 
					         ObjectID BIGINT
					        )

                            INSERT INTO #temp(ObjectID)
	                             " + sqlSelectColumnObjectPagingID +

                           @" IF ( @PageSize > 0
                                        AND @CurrentPage > 1
                                    ) 
                                     DELETE TOP 
                                        (@PageSize * ( @CurrentPage - 1 ))
                                     FROM #temp

                            " + sqlSelectResult;


            msg = DBM.ExecQueryString(strSql, parameters, out dt);
            if (msg.Length > 0) return msg;

            msg = DBM.ExecQueryString(sqlSelectTotal, parameters, out DataTable dtTotal);
            if (msg.Length > 0) return msg;
            if (dtTotal == null) return "dtTotal == null";
            if (dtTotal.Rows.Count != 1) return "dtTotal.Rows.Count != 1";
            if (!int.TryParse(dtTotal.Rows[0]["Total"].ToString(), out total)) return "!int.TryParse Total";

            return "";
        }
        private static string ValidateData<T>(string sqlInput, string columnObjectPagingID, T parameters) where T : class
        {
            if (string.IsNullOrEmpty(sqlInput)) return "Không được để trống sqlInput";
            if (string.IsNullOrEmpty(columnObjectPagingID)) return "Không được để trống columnObjectPagingID";

            string msg = CheckExistParameter(parameters, "PageSize", out bool exist);
            if (!exist) return "Chưa có tham số đầu vào PageSize";

            msg = CheckExistParameter(parameters, "CurrentPage", out exist);
            if (!exist) return "Chưa có tham số đầu vào CurrentPage ";

            return "";
        }
        private static string CheckExistParameter<T>(T parameters, string parameter, out bool exist) where T : class
        {
            exist = false;

            var ps = typeof(T).GetProperties();
            foreach (var p in ps)
                if (p.Name.Trim().ToLower() == parameter.Trim().ToLower()) { exist = true; break; }

            return "";
        }
        private static string GetSql(string sqlInput, string columnObjectPagingID, out string sqlSelectColumnObjectPagingID, out string sqlSelectResult, out string sqlSelectTotal)
        {
            sqlSelectColumnObjectPagingID = sqlSelectResult = sqlSelectTotal = "";

            string strSelect, strFrom, strWhere, strOrder;
            string msg = QueryStringBuilder.GetClausesSQL(sqlInput, out strSelect, out strFrom, out strWhere, out strOrder);
            if (msg.Length > 0) return msg;

            sqlSelectColumnObjectPagingID = string.Concat(new string[]
            {
                "SELECT TOP(@PageSize * @CurrentPage) ",
                columnObjectPagingID ,
                " ",
                strFrom,
                " ",
                strWhere,
                " ",
                strOrder
            });

            sqlSelectResult = string.Concat(new string[]
            {
                strSelect,
                " ",
                strFrom,
                " ",
                "WHERE " + columnObjectPagingID + " IN (SELECT ObjectID FROM #temp) ",
                strOrder
            });

            sqlSelectTotal = string.Concat(new string[]
            {
                "SELECT COUNT(1) Total ",
                strFrom,
                " ",
                strWhere
            });

            return "";
        }
    }
}