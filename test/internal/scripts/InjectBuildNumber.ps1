$assVersionPrefix = '[assembly: AssemblyFileVersion("'
$assVersionsuffix = '")]'

Function InjectBuildNumber
{
    Param ([string]$line)

    if ($line.StartsWith($assVersionPrefix))
    {
        $num = $line.Substring($assVersionPrefix.Length, $line.Length - $assVersionPrefix.Length - $assVersionsuffix.Length)
        $num_p = $num.Split('.')
        $new_num = [System.String]::Join('.', $num_p[0 .. 2] + $env:BUILD_NUMBER)
        $new_line = $assVersionPrefix + $new_num + $assVersionsuffix

        return $new_line
    }
    else
    {
        return $line
    }
}

Function UpdateAssemblyInfo
{
    Param ([string]$asspath)

    if ($env:BUILD_NUMBER)
    {

        $assinfo = Get-Content $asspath -Encoding UTF8

        $new_assinfo =  $assinfo | %{ InjectBuildNumber $_ }

        Set-Content -Path $asspath -Value $new_assinfo -Encoding UTF8
    }
}

UpdateAssemblyInfo ((Split-Path -Parent $PSCommandPath) + '\..\..\CLITest\Properties\AssemblyInfo.cs')