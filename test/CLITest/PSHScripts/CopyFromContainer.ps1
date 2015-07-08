param ([string]$modulePath,
[string]$accountName,
[string]$accountKey,
[string]$containerName,
[string]$shareName)


Import-Module $modulePath
$ctx = New-AzureStorageContext -StorageAccountName $accountName -StorageAccountKey $accountKey

$blobs = Get-AzureStorageBlob -Container $containerName -Context $ctx

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

function CopyFromBlob($blob)
{
    $destPath = $blob.Name;

    try
    {
        $share = Get-AzureStorageShare -Name $shareName -Context $ctx;
        $dir = GetDirReference $blob.Name $share;
        EnsureDirStructure $dir;
        Start-AzureStorageFileCopy -SrcBlob $blob -DestShareName $shareName -DestFilePath $destPath -DestContext $ctx;
    }
    catch
    {
        Write-Error "Failed to copy to " + $destPath + ": " + $_.Exception.Message;
    }
}

$blobs | %{ CopyFromBlob $_.ICloudBlob }