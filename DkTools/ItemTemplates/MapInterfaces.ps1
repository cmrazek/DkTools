param(
[Parameter(Mandatory=$true)][string]$outputFileName,
[string]$appName = ""
)

$ErrorActionPreference = "Stop";

[string]$dictsrvrLib = $null;
[string]$probeenvsrvrLib = $null;
foreach ($pathDir in $env:PATH.Split(';'))
{
    if (!$dictsrvrLib -and [System.IO.File]::Exists("$pathDir\Interop.DICTSRVRLib.dll"))
    {
        $dictsrvrLib = "$pathDir\Interop.DICTSRVRLib.dll";
    }
    if (!$probeenvsrvrLib -and [System.IO.File]::Exists("$pathDir\Interop.PROBEENVSRVRLib.dll"))
    {
        $probeenvsrvrLib = "$pathDir\Interop.PROBEENVSRVRLib.dll";
    }
}

if (!$dictsrvrLib) { throw "Interop.DICTSRVRLib.dll could not be located."; }
if (!$probeenvsrvrLib) { throw "Interop.PROBEENVSRVRLib.dll could not be located."; }

Add-Type -Path $dictsrvrLib;
Add-Type -Path $probeenvsrvrLib;

function WriteDataType($dataType)
{
    switch ($dataType.Type)
    {
        0 { return "void"; }
        1 { return "char"; }
        2 { return "char($($dataType.Length))"; }
        3 { return "int"; }
        4 { return "unsigned"; }
        5 { return "numeric($($dataType.Scale),$($dataType.Precision))"; }
        6 { return "date"; }
        7 { return "time"; }
        8 {
            [string]$str = "enum { ";
            [int]$enumCount = $dataType.Enumcount;
            for ([int]$e = 1; $e -le $enumCount; $e++)
            {
                if ($e -ne 1) { $str += ", "; }
                $str += $dataType.Enumitem(0, $e).Trim();
            }
            $str += " }";
            return $str;
        }
        #11 { return "graphic"; }
        12 { return "unsigned char"; }
        13 { return "int"; }
        14 { return "unsigned int"; }
        15 { return "short"; }
        16 { return "unsigned short"; }
        17 { return "long"; }
        18 { return "unsigned long"; }
        #19 { return "scroll"; }
        #20 { return "command"; }
        #21 { return "section"; }
        22 { return "interface $($dataType.InterfaceName)"; }
        23 { return "variant"; }
        24 { return "longchar($($dataType.Length))"; }
        #25 { return "binary"; }
        #26 { return "MultiImage(0)"; }
        32767 { return $dataType.BaseTypeDefine; }
        65535 { return "UNDEFINED"; }
    }

    return "void";
}

$out = New-Object System.IO.StreamWriter($outputFileName);
try
{
    [int]$intfProcessed = 0;

    $env = New-Object PROBEENVSRVRLib.ProbeEnvClass;
    if (!$appName) { $appName = $env.DefaultAppName; }
    $out.WriteLine("// App Name: $appName");

    $app = $env.FindApp($appName);
    if ($app -eq $null) { throw "App is null."; }

    $repo = New-Object DICTSRVRLib.PRepositoryClass;
    if ($repo -eq $null) { throw "Repo is null."; }

    $dict = $repo.LoadDictionary($app, "", 2);
    if ($dict -eq $null) { throw "Dict is null."; }

    [int]$intfCount = $dict.InterfaceTypeCount;
    for ([int]$i = 1; $i -le $intfCount; $i++)
    {
        $intfProcessed++;
        $intf = $dict.InterfaceTypes($i);
        $out.WriteLine("#interface $($intf.Name)");
        $out.WriteLine("{");

        [int]$methodCount = $intf.MethodCount;
        for ([int]$m = 1; $m -le $methodCount; $m++)
        {
            [string]$sig = "    ";

            $sig += WriteDataType $intf.MethodDataDef($m);
            $sig += " ";
            $sig += $intf.MethodName($m);
            $sig += "(";

            [int]$paramCount = $intf.MethodParamCount($m);
            for ([int]$p = 1; $p -le $paramCount; $p++)
            {
                if ($p -ne 1) { $sig += ", "; }
                $sig += WriteDataType $intf.MethodParamDataDef($m, $p);

                [string]$paramName = $intf.MethodParamName($m, $p);
                if ($paramName)
                {
                    $sig += " ";
                    $sig += $paramName;
                }
            }
            $sig += ");";

            $out.WriteLine($sig);
        }

        [int]$propCount = $intf.PropertyCount;
        for ([int]$p = 1; $p -le $propCount; $p++)
        {
            [string]$sig = "    ";

            $dataType = $intf.PropertyDataDef($p);
            $sig += WriteDataType $dataType;
            $sig += " ";
            $sig += $intf.PropertyName($p);
            if ($dataType.Readonly)
            {
                $sig += " readonly";
            }
            $sig += ";";

            $out.WriteLine($sig);
        }

        $out.WriteLine("}");
    }

    Write-Output "Interface Count: $intfProcessed";
}
finally
{
    $out.Flush();
    $out.Close();
    $out = $null;
}
