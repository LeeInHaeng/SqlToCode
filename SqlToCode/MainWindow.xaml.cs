using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static SqlToCode.MainWindow;

namespace SqlToCode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModalToModel modalToModelDialog;
        private ModalToRepository modalToRepositoryDialog;
        public LoadTableInfo loadTable;
        public SqlBlockData sqlBLockData = null;

        #region Model

        public class SqlBlockData
        {
            public string _comment          { get; set; }
            public string _expectedSpType   { get; set; }
            public string _spPrefix         { get; set; }
            public string _spName           { get; set; }
            public List<ProcedureData> _spParamList { get; set; }
            public List<SelectData> _selectDataList { get; set; } // select 일때만 데이터 있음

            public SqlBlockData()
            {
                _spParamList = new List<ProcedureData>();
                _selectDataList = new List<SelectData>();
            }
        }

        public class SelectData
        {
            public List<SpSelectColumnInfo> _columnList { get; set; }
            public List<SpSelectTableInfo> _tableList { get; set; }

            public SelectData()
            {
                _columnList = new List<SpSelectColumnInfo>();
                _tableList = new List<SpSelectTableInfo>();
            }
        }

        public class ProcedureData
        {
            public string   _parameterName { get; set; }
            public string   _parameterType { get; set; }
            public bool     _isOutput      { get; set; }
        }

        public class LoadTableInfo
        {
            // key : tableName , value : column List
            public Dictionary<string, List<TableColumnInfo>> _tableInfo { get; set; }

            public LoadTableInfo()
            {
                _tableInfo = new Dictionary<string, List<TableColumnInfo>>();
            }
        }

        public class TableColumnInfo
        {
            public string _columnName { get; set; }
            public string _columnType { get; set; }
        }

        public class SpSelectColumnInfo
        {
            public string _orgName { get; set; }
            public string _aliasName { get; set; }
        }

        public class SpSelectTableInfo
        {
            public string _orgName { get; set; }
            public string _aliasName { get; set; }
        }

        #endregion

        public MainWindow()
        {
            loadTable = new LoadTableInfo();
            InitializeComponent();
        }

        #region Dialog

        private void ModalToModelDialogShow()
        {
            sqlBLockData = MakeSqlBlockData();

            modalToModelDialog = new ModalToModel();
            modalToModelDialog.Owner = this;

            string dialogData = MakeClass(sqlBLockData);

            modalToModelDialog.DialogData = dialogData;
            modalToModelDialog.ShowDialog();

            // modalToModel dialog 닫힐때 다시 focus
            TextBox_SqlBlock.Focus();
            TextBox_SqlBlock.SelectAll();
        }

        private void ModalToRepositoryDialogShow()
        {
            sqlBLockData = MakeSqlBlockData();

            modalToRepositoryDialog = new ModalToRepository();
            modalToRepositoryDialog.Owner = this;

            string dialogData = MakeRepository(sqlBLockData);

            modalToRepositoryDialog.DialogData = dialogData;
            modalToRepositoryDialog.ShowDialog();

            // modalToModel dialog 닫힐때 다시 focus
            TextBox_SqlBlock.Focus();
            TextBox_SqlBlock.SelectAll();
        }

        #endregion

        #region Event Handler

        private void Btn_ToModel_Click(object sender, RoutedEventArgs e)
        {
            ModalToModelDialogShow();

            // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
            e.Handled = true;
        }

        private void Btn_ToRepository_Click(object sender, RoutedEventArgs e)
        {
            ModalToRepositoryDialogShow();

            // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
            e.Handled = true;
        }

        private void OnKeyDownSqlBlock(object sender, KeyEventArgs e)
        {
            // Ctrl + Q 키 입력
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Q)
            {
                ModalToModelDialogShow();

                // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
                e.Handled = true;
            }

            // Ctrl + W 키 입력
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.W)
            {
                ModalToRepositoryDialogShow();

                // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
                e.Handled = true;
            }
        }

        private void OnLoadSqlBlock(object sender, RoutedEventArgs e)
        {
            TextBox_SqlBlock.Focus();
            TextBox_SqlBlock.SelectAll();

            // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
            e.Handled = true;
        }

        private void OnLostFocusSqlCodePath(object sender, RoutedEventArgs e)
        {
            string codePath = TextBox_SqlCodePath.Text;
            RecursiveSqlFileLoad(codePath);

            // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
            MessageBox.Show("SQL 파일 로드 완료");
            e.Handled = true;
        }

        private void RecursiveSqlFileLoad(string codePath)
        {
            if (true == string.IsNullOrEmpty(codePath))
            {
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(codePath);

            DirectoryInfo[] directoryList = directoryInfo.GetDirectories();
            if (0 < directoryList.Length)
            {
                foreach (DirectoryInfo directory in directoryList) 
                {
                    RecursiveSqlFileLoad(directory.FullName);
                }
            }

            FileInfo[] sqlFileList = directoryInfo.GetFiles("*.sql");
            if (sqlFileList.Length <= 0)
            {
                return;
            }

            foreach (FileInfo sqlFile in sqlFileList)
            {
                string contents = string.Empty;
                bool isParamExtractStart = false;
                string tmpTblName = string.Empty;
                List<TableColumnInfo> columnList = new List<TableColumnInfo>();

                using (StreamReader reader = sqlFile.OpenText())
                {
                    while (null != (contents = reader.ReadLine()))
                    {
                        contents = contents.Replace(",", "").Replace("\t", "    ").Trim();


                        if (true == "(".Equals(contents))
                        {
                            continue;
                        }

                        if (true == isParamExtractStart && (")".Equals(contents) || ");".Equals(contents)))
                        {
                            loadTable._tableInfo.Add(tmpTblName, columnList);

                            isParamExtractStart = false;
                            continue;
                        }

                        // 공백을 기준으로 추출
                        // 컬럼명 {공백 1개 이상} 데이터타입 {공백 1개 이상} ...
                        if (true == isParamExtractStart)
                        {
                            TableColumnInfo columnInfo = new TableColumnInfo();
                            columnInfo._columnName = contents.Substring(0, contents.IndexOf(" "));
                            contents = contents.Replace(columnInfo._columnName, "").Trim();
                            columnInfo._columnType = contents.Substring(0, contents.IndexOf(" ")).ToLower();
                            columnList.Add(columnInfo);
                        }

                        if (true == contents.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
                        {
                            tmpTblName = contents.Replace("CREATE TABLE", "", StringComparison.OrdinalIgnoreCase).Trim();
                            if (true == loadTable._tableInfo.ContainsKey(tmpTblName))
                            {
                                continue;
                            }

                            isParamExtractStart = true;
                            columnList = new List<TableColumnInfo>();
                        }
                    }
                }
            }
        }

        #endregion

        #region Service

        private SqlBlockData MakeSqlBlockData()
        {
            SqlBlockData result = null;

            string orgSqlBlockText = string.Empty;
            string[] lineSqlBlockText;

            // 추출 순서
            // SP 이름 추출(isSpNameExtract) --> SP 이름을 바탕으로 C R U D 인지 추출(isExpectedSpTypeExtract)
            // --> 다음 라인부터 파라미터 추출 (isStartParamExtract) --> brief 에 적힌 comment 추출
            // --> select 일 경우 "작업시작" 이하 추가 스캔 필요 (isSelectExtract)
            bool isSpNameExtract = false;
            bool isExpectedSpTypeExtract = false;
            bool isStartParamExtract = false;
            bool isCommentExtract = false;

            bool isSelectExtract = false;
            bool isSelectColumnResultExtract = false;
            bool isSelectTableResultExtract = false;
            List<SpSelectColumnInfo> selectColumnList = new List<SpSelectColumnInfo>();
            List<SpSelectTableInfo> selectTableList = new List<SpSelectTableInfo>();

            string tmpSpNameData = string.Empty;
            string[] tmpSpNameDataArr;

            ProcedureData tmpSpParamData;

            try
            {
                orgSqlBlockText = TextBox_SqlBlock.Text;
                if (true == string.IsNullOrEmpty(orgSqlBlockText))
                {
                    MessageBox.Show($"MakeSqlBlockData Textbox contents Empty");
                    return result;
                }

                result = new SqlBlockData();

                // 데이터 가공
                lineSqlBlockText = orgSqlBlockText.Replace("\r", "").Replace("\t", "    ").Split('\n');

                foreach (string sqlBlockText in lineSqlBlockText)
                {
                    #region SP Name 추출
                    // spName 추출
                    if (true == sqlBlockText.Contains("create procedure", StringComparison.OrdinalIgnoreCase))
                    {
                        tmpSpNameData = sqlBlockText.Replace("create procedure", "", StringComparison.OrdinalIgnoreCase).Trim().Replace("[", "").Replace("]", "");
                        tmpSpNameDataArr = tmpSpNameData.Split('.');

                        if (tmpSpNameDataArr.Length != 2)
                        {
                            MessageBox.Show($"MakeSqlBlockData spName Extract fail 1 : {sqlBlockText}");
                            return result;
                        }

                        result._spPrefix = tmpSpNameDataArr[0];
                        result._spName = tmpSpNameDataArr[1];

                        isSpNameExtract = true;
                    }

                    if (true == isSpNameExtract && true == string.IsNullOrEmpty(result._spName))
                    {
                        MessageBox.Show($"MakeSqlBlockData spName Extract fail 2 : {sqlBlockText}");
                        return result;
                    }
                    #endregion

                    #region SP C/R/U/D 추출
                    // _expectedSpType 추출 ( c r u d )
                    if (true == isSpNameExtract)
                    {
                        if (true == result._spName.ToLower().Contains("update")
                            || true == result._spName.ToLower().Contains("delete"))
                        {
                            result._expectedSpType = "update";
                            isExpectedSpTypeExtract = true;
                        }
                        else if (true == result._spName.ToLower().Contains("insert"))
                        {
                            result._expectedSpType = "insert";
                            isExpectedSpTypeExtract = true;
                        }
                        else
                        {
                            result._expectedSpType = "select";
                            isExpectedSpTypeExtract = true;
                        }

                        isSpNameExtract = false;
                    }

                    if (true == isExpectedSpTypeExtract && true == string.IsNullOrEmpty(result._expectedSpType))
                    {
                        MessageBox.Show($"MakeSqlBlockData expectedSpType Extract fail. spName : {result._spName}");
                        return result;
                    }
                    #endregion

                    #region SP 인풋,아웃풋 파라미터 추출
                    // sp 파라미터 추출 : create procedure 와 as 사이
                    if (true == isStartParamExtract)
                    {
                        tmpSpParamData = new ProcedureData();

                        string tmpSqlBlockText = sqlBlockText.Replace(",", "").Trim();
                        if (true == "AS".Equals(tmpSqlBlockText, StringComparison.OrdinalIgnoreCase))
                        {
                            isStartParamExtract = false;
                            isCommentExtract = true;
                        }
                        else
                        {
                            tmpSpParamData._parameterName = tmpSqlBlockText.Substring(tmpSqlBlockText.IndexOf("@"), tmpSqlBlockText.IndexOf(" "));
                            tmpSqlBlockText = tmpSqlBlockText.Replace(tmpSpParamData._parameterName, "").Trim();
                            tmpSpParamData._isOutput = (true == tmpSqlBlockText.Contains(" output", StringComparison.OrdinalIgnoreCase));
                            if (true == tmpSpParamData._isOutput)
                            {
                                tmpSqlBlockText = tmpSqlBlockText.Replace(" output", "", StringComparison.OrdinalIgnoreCase).Trim();
                            }
                            if (tmpSqlBlockText.IndexOf(" ") < 0)
                            {
                                tmpSpParamData._parameterType = tmpSqlBlockText.ToLower();
                            }
                            else
                            {
                                tmpSpParamData._parameterType = tmpSqlBlockText.Substring(0, tmpSqlBlockText.IndexOf(" ")).ToLower();
                            }

                            result._spParamList.Add(tmpSpParamData);
                        }
                    }
                    #endregion


                    #region select 일 경우 안의 프로시저 내용까지 확인해서 추출 필요
                    // select 추가 추출 필요할 경우 추출 시작
                    if (true == isSelectExtract)
                    {
                        // select 문구 시작시 from 전까지 추출 (totalCount 제외 , exists 체크 제외 , 변수를 담기위한 select 제외 , FROM 에 들어가는 서브쿼리 제외
                        if (true == sqlBlockText.Contains("select ", StringComparison.OrdinalIgnoreCase)
                            && false == sqlBlockText.Contains("@totalCount", StringComparison.OrdinalIgnoreCase)
                            && false == sqlBlockText.Contains(" exists ", StringComparison.OrdinalIgnoreCase)
                            && false == sqlBlockText.Contains(" @")
                            && false == sqlBlockText.Contains(" FROM (", StringComparison.OrdinalIgnoreCase))
                        {
                            isSelectColumnResultExtract = true;
                            selectColumnList = new List<SpSelectColumnInfo>();
                            selectTableList = new List<SpSelectTableInfo>();
                        }
                    }

                    if (true == isSelectExtract && true == isSelectColumnResultExtract)
                    {
                        string trimSqlBlockText = sqlBlockText.Replace("select ", "", StringComparison.OrdinalIgnoreCase).Replace("," , "").Trim();
                        if (true == trimSqlBlockText.Contains("FROM ", StringComparison.OrdinalIgnoreCase)
                            || true == trimSqlBlockText.Contains("JOIN ", StringComparison.OrdinalIgnoreCase))
                        {
                            isSelectColumnResultExtract = false;
                            isSelectTableResultExtract = true;
                        }
                        else
                        {
                            // 공백이 있을 경우 별칭 써서 반환 하는 케이스
                            if (0 < trimSqlBlockText.IndexOf(" "))
                            {
                                trimSqlBlockText = trimSqlBlockText.Replace("AS", "", StringComparison.OrdinalIgnoreCase);

                                string[] splitedTrimSqlBlockText = trimSqlBlockText.Split(' ');
                                SpSelectColumnInfo selectColumnInfo = new SpSelectColumnInfo();
                                selectColumnInfo._orgName = splitedTrimSqlBlockText[0].Trim();
                                selectColumnInfo._aliasName = trimSqlBlockText.Replace(selectColumnInfo._orgName, "").Trim();

                                selectColumnList.Add(selectColumnInfo);
                            }
                            else
                            {
                                selectColumnList.Add(new SpSelectColumnInfo
                                {
                                    _orgName = trimSqlBlockText
                                    ,_aliasName = string.Empty
                                });
                            }
                        }
                    }

                    if (true == isSelectExtract && true == isSelectTableResultExtract)
                    {
                        string trimSqlBlockText = sqlBlockText.Replace("FROM ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("INNER JOIN ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("LEFT JOIN ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("LEFT OUTER JOIN ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("RIGHT JOIN ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("RIGHT OUTER JOIN ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("CROSS JOIN ", "", StringComparison.OrdinalIgnoreCase)
                                                            .Trim();
                        // JOIN 절 사용할때 ON 인 경우 제외
                        if (true == trimSqlBlockText.Contains("ON ", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        // select 문구 하나 끝나는 조건
                        if (true == trimSqlBlockText.Contains("WHERE ", StringComparison.OrdinalIgnoreCase)
                            || true == trimSqlBlockText.Contains("ORDER BY ", StringComparison.OrdinalIgnoreCase)
                            || true == trimSqlBlockText.Contains("OFFSET ", StringComparison.OrdinalIgnoreCase)
                            || true == trimSqlBlockText.Contains("GROUP BY ", StringComparison.OrdinalIgnoreCase))
                        {
                            isSelectTableResultExtract = false;
                            result._selectDataList.Add(new SelectData
                            {
                                _columnList = selectColumnList
                                ,_tableList = selectTableList
                            });

                            selectColumnList = new List<SpSelectColumnInfo>();
                            selectTableList = new List<SpSelectTableInfo>();

                            continue;
                        }
                        // sp 완전 종료 조건
                        if(true == trimSqlBlockText.Contains("END TRY ", StringComparison.OrdinalIgnoreCase)
                           || true == trimSqlBlockText.Contains("/******"))
                        {
                            break;
                        }

                        // 공백이 있을 경우 별칭 써서 반환 하는 케이스
                        if (0 < trimSqlBlockText.IndexOf(" "))
                        {
                            trimSqlBlockText = trimSqlBlockText.Replace("AS", "", StringComparison.OrdinalIgnoreCase);

                            string[] splitedTrimSqlBlockText = trimSqlBlockText.Split(' ');
                            SpSelectTableInfo selectTableInfo = new SpSelectTableInfo();
                            selectTableInfo._orgName = splitedTrimSqlBlockText[0].Trim();
                            selectTableInfo._aliasName = trimSqlBlockText.Replace(selectTableInfo._orgName, "").Trim();

                            selectTableList.Add(selectTableInfo);
                        }
                        else
                        {
                            selectTableList.Add(new SpSelectTableInfo
                            {
                                _orgName = trimSqlBlockText
                                ,_aliasName = string.Empty
                            });
                        }
                    }
                    #endregion


                    #region 주석(comment) 추출 : select 가 아닐 경우 추출 끝
                    // comment 추출
                    if (true == isCommentExtract && true == sqlBlockText.Contains("\\brief", StringComparison.OrdinalIgnoreCase))
                    {
                        result._comment = sqlBlockText.Replace("*", "").Replace("\\brief", "", StringComparison.OrdinalIgnoreCase).Trim();
                        isCommentExtract = false;
                        // select 일 경우 추가 추출 필요
                        if (true == result._expectedSpType.Contains("select", StringComparison.OrdinalIgnoreCase))
                        {
                            isSelectExtract = true;
                        }
                        // select 가 아닐 경우 필요한 정보 다 추출 완료
                        else
                        {
                            break;
                        }
                    }
                    #endregion

                    if (true == isExpectedSpTypeExtract)
                    {
                        // 다음 라인부터 create procedure 이후의 파라미터들 추출
                        isStartParamExtract = true;
                        isExpectedSpTypeExtract = false;
                    }
                }

            }
            catch (Exception exception)
            {
                MessageBox.Show($"MakeSqlBlockData Exception : {exception.ToString()}");
                result = null;
            }

            return result;
        }

        private string GetDbTypeToCSharp(string dbType)
        {
            if (true == dbType.Contains("varchar"))
            {
                return "string    ";
            }
            else if (true == dbType.Contains("date"))
            {
                return "DateTime  ";
            }
            else if(true == dbType.Equals("tinyint"))
            {
                return "byte      ";
            }
            else if(true == dbType.Equals("int"))
            {
                return "int       ";
            }
            else if (true == dbType.Equals("bigint"))
            {
                return "long      ";
            }
            else if (true == dbType.Equals("bit"))
            {
                return "bool      ";
            }

            return dbType;
        }

        // 하나의 Data 클래스에 대한것
        private string MakeDataClass(SqlBlockData sqlBlockData, SelectData selectData)
        {
            string result = string.Empty;

            string commonClassName = string.Empty;
            string resultProperty = string.Empty;
            List<string> duplicatedProperty = new List<string>();

            try
            {
                //// 공통
                commonClassName = sqlBlockData._spName.Replace("usp", "", StringComparison.OrdinalIgnoreCase);

                result = $"public class {commonClassName}Data\n";
                result += "{\n";

                foreach (SpSelectColumnInfo column in selectData._columnList)
                {
                    resultProperty = string.Empty;

                    resultProperty += "    public ";
                    // 테이블 혹은 테이블 alias 가 붙음
                    if (0 < column._orgName.IndexOf("."))
                    {

                    }
                    // FROM 에 있는 테이블에 매칭되는 컬럼 그대로 사용
                    else
                    {
                        List<TableColumnInfo> columnList = loadTable._tableInfo[selectData._tableList[0]._orgName];
                        string propertyName = string.Empty;

                        string dbColumnType = columnList.Where(m => true == m._columnName.Equals(column._orgName, StringComparison.OrdinalIgnoreCase)).First()._columnType;

                        resultProperty += $"{GetDbTypeToCSharp(dbColumnType)} ";
                        propertyName = (true == string.IsNullOrEmpty(column._aliasName) ? column._orgName : column._aliasName);
                        if (true == duplicatedProperty.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        else
                        {
                            resultProperty += propertyName;
                            duplicatedProperty.Add(propertyName);
                        }

                        resultProperty += " { get; set; }\n";
                        result += resultProperty;
                    }
                }

                result += "}";
            }
            catch (Exception exception)
            {
                MessageBox.Show($"MakeDataClass Exception : {exception.ToString()}");
                result = string.Empty;
            }

            return result;
        }

        private string MakeClass(SqlBlockData sqlBlockData)
        {
            string result = string.Empty;

            string commonClassName = string.Empty;
            string paramClass = string.Empty;
            string resultClass = string.Empty;
            string dataClass = string.Empty;

            try
            {
                //// 공통
                commonClassName = sqlBlockData._spName.Replace("usp", "", StringComparison.OrdinalIgnoreCase);

                // 파라미터 class 셋팅
                paramClass = $"public class {commonClassName}Param";
                paramClass += "\n{\n";
                foreach (ProcedureData procedureData in sqlBlockData._spParamList)
                {
                    if (true == procedureData._isOutput)
                    {
                        continue;
                    }

                    paramClass += $"    public {GetDbTypeToCSharp(procedureData._parameterType)}{procedureData._parameterName.Replace("@", "_")}" + " { get; set; }\n";
                }
                paramClass += "}";

                // 결과 class 셋팅
                resultClass = $"public class {commonClassName}Result";
                resultClass += "\n{\n";
                resultClass += "    public int       _resultCode { get; set; }\n";
                foreach (ProcedureData procedureData in sqlBlockData._spParamList)
                {
                    if (true == procedureData._parameterName.Equals("@resultcode", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (false ==  procedureData._isOutput)
                    {
                        continue;
                    }

                    resultClass += $"    public {GetDbTypeToCSharp(procedureData._parameterType)}{procedureData._parameterName.Replace("@", "_")}" + " { get; set; }\n";
                }

                // select 절일 경우 더 추가적으로 필요
                // 조회하는 컬럼은 orgName 으로 추출 필요하고, 만들어야 하는 결과는 alias --> alias 없으면 orgName

                // grid reader 일 경우 추가 고려 필요
                if (true == "select".Equals(sqlBlockData._expectedSpType) && 1 < sqlBlockData._selectDataList.Count)
                {

                }
                else if (true == "select".Equals(sqlBlockData._expectedSpType) && 1 == sqlBlockData._selectDataList.Count)
                {
                    resultClass += $"    public List<{commonClassName}Data> _list" + " { get; set; }\n\n";
                    resultClass += $"    public {commonClassName}Result()\n";
                    resultClass += "    {\n";
                    resultClass += $"        _list = new List<{commonClassName}Data>();\n";
                    resultClass += "    }\n";
                }

                resultClass += "}";

                result = $"{paramClass}\n\n{resultClass}";

                // grid reader 일 경우 추가 고려 필요
                if (true == "select".Equals(sqlBlockData._expectedSpType) && 1 < sqlBlockData._selectDataList.Count)
                {

                }
                else if (true == "select".Equals(sqlBlockData._expectedSpType) && 1 == sqlBlockData._selectDataList.Count)
                {
                    dataClass = MakeDataClass(sqlBlockData, sqlBlockData._selectDataList[0]);
                }

                if (true == "select".Equals(sqlBlockData._expectedSpType))
                {
                    result += $"\n\n{dataClass}";
                }
            }
            catch (Exception exception) 
            {
                MessageBox.Show($"MakeClass Exception : {exception.ToString()}");
                result = string.Empty;
            }

            return result;
        }

        private string MakeRepository(SqlBlockData sqlBlockData)
        {
            string result = string.Empty;

            string commonClassName = string.Empty;

            try
            {
                //// 공통
                commonClassName = sqlBlockData._spName.Replace("usp", "", StringComparison.OrdinalIgnoreCase);

                result += $"public {commonClassName}Result {commonClassName} ({commonClassName}Param model)";
                result += "{";
                result += $"    {commonClassName}Result result = new {commonClassName}Result();";
            }
            catch (Exception exception)
            {
                MessageBox.Show($"MakeRepository Exception : {exception.ToString()}");
                result = string.Empty;
            }

            return result;
        }

        #endregion
    }
}
