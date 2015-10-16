param ([string]$sourceConnectionString,
[string]$destConnectionString,
[string]$sourceShareName,
[string]$destShareName)


$sourceCtx = New-AzureStorageContext -ConnectionString $sourceConnectionString
$destCtx = New-AzureStorageContext -ConnectionString $destConnectionString

$sourceFiles = Get-AzureStorageFile -ShareName $sourceShareName -Context $sourceCtx

function EnsureDirStructure ($dir)
{
    if ($dir.Exists() -ne $true)
    {
        if ( $dir.Parent -ne $null )
        {
            EnsureDirStructure ( $dir.Parent );
        }

        $dir.Create();
    }
}

function GetDirReference ($dirPath, $shareRef)
{
    $dir = $shareRef.GetRootDirectoryReference();

    $lastIndex = 0;

    $separatorIndex = $dirPath.IndexOf("/");

    While ($separatorIndex -ne -1)
    {
        $dir = $dir.GetDirectoryReference($dirPath.SubString($lastIndex, $separatorIndex - $lastIndex));
        $lastIndex = $separatorIndex + 1;

        $separatorIndex = $dirPath.IndexOf("/", $lastIndex);
    }

    return $dir;
}

function GetDirFullPath($dir)
{
    $fullPath = "";
    if ($dir.Parent -ne $null)
    {
        $fullPath = GetDirFullPath($dir.Parent);
    }

    if ($fullPath -ne "")
    {
        return $fullPath + "/" + $dir.Name;
    }
    else
    {
        return $dir.Name;
    }
}

function GetFullPath($file)
{
    $dirFullPath = GetDirFullPath($file.Parent);

    if ($dirFullPath -ne "")
    {
        return $dirFullPath + "/" + $file.Name;
    }
    else
    {
        return $file.Name;
    }
}

$share = Get-AzureStorageShare -Name $destShareName -Context $destCtx;

function CopyFromFile($sourceFile)
{ 
    if ( $_.GetType().Name -eq "CloudFile" )
    {
        $destPath = GetFullPath($_);

        try
        {
            $dir = GetDirReference $destPath $share;
            EnsureDirStructure $dir;
            Start-AzureStorageFileCopy -SrcFile $_ -DestShareName $destShareName -DestFilePath $destPath -DestContext $destCtx -Force;
        }
        catch
        {            
            Write-Error "Failed to copy to " + $destPath + ": " + $_.Exception.Message;
        }
    }
}

$sourceFiles | %{ CopyFromFile $_ }