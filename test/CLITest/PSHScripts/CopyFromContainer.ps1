param ([string]$modulePath,
[string]$sourceConnectionString,
[string]$destConnectionString,
[string]$containerName,
[string]$shareName)


Import-Module $modulePath
$sourceCtx = New-AzureStorageContext -ConnectionString $sourceConnectionString
$destCtx = New-AzureStorageContext -ConnectionString $destConnectionString

$blobs = Get-AzureStorageBlob -Container $containerName -Context $sourceCtx

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

$share = Get-AzureStorageShare -Name $shareName -Context $destCtx;

function CopyFromBlob($blob)
{
    $destPath = $blob.Name;

    try
    {
        $dir = GetDirReference $blob.Name $share;
        EnsureDirStructure $dir;
        Start-AzureStorageFileCopy -SrcBlob $blob -DestShareName $shareName -DestFilePath $destPath -DestContext $destCtx -Force;
    }
    catch
    {
        Write-Error "Failed to copy to " + $destPath + ": " + $_.Exception.Message;
    }
}

$blobs | %{ CopyFromBlob $_.ICloudBlob }