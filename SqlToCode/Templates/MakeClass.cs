namespace SqlToCode.Templates
{
    public static class MakeClassTemplate
    {
        public static string ParamTemplate()
        {
return
@"public class [REPLACE:SPNAME]Param
{
[REPLACE:PARAMLIST]
}
";
        }

        public static string ResultTemplate(string type)
        {
            switch (type)
            {
                case "select":
return
@"public class [REPLACE:SPNAME]Result
{
    public int       _resultCode { get; set; }
[REPLACE:RESULTLIST]
    public List<[REPLACE:SPNAME]Data> _list { get; set; }

    public [REPLACE:SPNAME]Result()
    {
        _list = new List<[REPLACE:SPNAME]Data>();
    }
}

public class [REPLACE:SPNAME]Data
{
[REPLACE:DATALIST]
}
";
                default:
return
@"public class [REPLACE:SPNAME]Result
{
    public int       _resultCode { get; set; }
[REPLACE:RESULTLIST]
}
";
            }    
        }
    }
}
