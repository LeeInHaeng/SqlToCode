using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToCode.Templates
{
    public static class MakeRepositoryTemplate
    {
        public static string RepositorySelectTemplate()
        {
return
@"/// <summary>
/// [REPLACE:COMMENT]
/// </summary>
/// <param name=""model""></param>
/// <returns></returns>
public [REPLACE:SPNAME]Result [REPLACE:SPNAME] ([REPLACE:SPNAME]Param model)
{
    [REPLACE:SPNAME]Result result = new [REPLACE:SPNAME]Result();
    InputOutputParameters param = new InputOutputParameters();
    OutputParameters<[REPLACE:SPNAME]Data> spResult = null;

    try
    {
[REPLACE:REPO_PARAMLIST]
        spResult = Execute<[REPLACE:SPNAME]Data>(""[REPLACE:FULL_SPNAME]"", param);
        if (null != spResult)
        {
            result._resultCode = spResult._resultCode;
            if (null != spResult._resultList && 0 < spResult._resultList.Count)
            {
                result._list = spResult._resultList;
            }
[REPLACE:OUTPUT_RESULT]
        }
    }
    catch (Exception exception)
    {
        LogUtil.Error($""[REPLACE:SPNAME] Exception : {exception.ToString()}"", ""ERROR"");
        result = new [REPLACE:SPNAME]Result();
        result._resultCode = (int)EnumManager.ErrorCode.exception;
    }

    return result;
}
";
        }

        public static string RepositoryCUDTemplate()
        {
return
@"/// <summary>
/// [REPLACE:COMMENT]
/// </summary>
/// <param name=""model""></param>
/// <returns></returns>
public [REPLACE:SPNAME]Result [REPLACE:SPNAME] ([REPLACE:SPNAME]Param model)
{
    [REPLACE:SPNAME]Result result = new [REPLACE:SPNAME]Result();
    InputOutputParameters param = new InputOutputParameters();
    OutputParameters spResult = null;

    try
    {
[REPLACE:REPO_PARAMLIST]
        spResult = Execute(""[REPLACE:FULL_SPNAME]"", param);
        if (null != spResult)
        {
            result._resultCode = spResult._resultCode;
[REPLACE:OUTPUT_RESULT]
        }
    }
    catch (Exception exception)
    {
        LogUtil.Error($""[REPLACE:SPNAME] Exception : {exception.ToString()}"", ""ERROR"");
        result = new [REPLACE:SPNAME]Result();
        result._resultCode = (int)EnumManager.ErrorCode.exception;
    }

    return result;
}
";
        }
    }
}
