# IIS Deployment Guide for PMS Application

This guide provides step-by-step instructions to deploy the PMS application to IIS.

## Prerequisites

- Windows Server with IIS installed
- .NET 9.0 Hosting Bundle installed
- SQL Server accessible from the server
- Administrator privileges

## Quick Deployment (Automated)

Run the automated deployment script:

```powershell
cd D:\PMS_Coditium
powershell -ExecutionPolicy Bypass -File "deploy-to-iis.ps1"
```

## Manual Deployment Steps

### Step 1: Build the Project

```powershell
cd D:\PMS_Coditium
dotnet build --configuration Release
```

### Step 2: Publish the Project

```powershell
dotnet publish --configuration Release --output "D:\PMSDeploy" --self-contained false
```

**Note:** If files are locked by IIS, stop the application pool and website first:
```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name "PMSAppPool"
Stop-Website -Name "PMS"
```

### Step 3: Create IIS Application Pool

```powershell
Import-Module WebAdministration

# Remove existing pool if it exists
Remove-WebAppPool -Name "PMSAppPool" -ErrorAction SilentlyContinue

# Create new application pool
New-WebAppPool -Name "PMSAppPool"

# Configure for .NET Core (No Managed Code)
Set-ItemProperty -Path "IIS:\AppPools\PMSAppPool" -Name managedRuntimeVersion -Value ""

# Set identity
Set-ItemProperty -Path "IIS:\AppPools\PMSAppPool" -Name processModel.identityType -Value "ApplicationPoolIdentity"

# Set start mode
Set-ItemProperty -Path "IIS:\AppPools\PMSAppPool" -Name startMode -Value "AlwaysRunning"
```

### Step 4: Create IIS Website

```powershell
# Remove existing website if it exists
Remove-Website -Name "PMS" -ErrorAction SilentlyContinue

# Create new website
New-Website -Name "PMS" `
            -PhysicalPath "D:\PMSDeploy" `
            -ApplicationPool "PMSAppPool" `
            -IPAddress "172.20.228.2" `
            -Port 84 `
            -Force
```

### Step 5: Verify web.config

Ensure `D:\PMSDeploy\web.config` exists with the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\PMS.Web.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### Step 6: Start Application Pool and Website

```powershell
Start-WebAppPool -Name "PMSAppPool"
Start-Website -Name "PMS"
```

### Step 7: Verify Deployment

Open a browser and navigate to: `http://172.20.228.2:84`

## Troubleshooting

### Check Application Pool Status

```powershell
Get-WebAppPoolState -Name "PMSAppPool"
```

### Check Website Status

```powershell
Get-WebsiteState -Name "PMS"
```

### View Application Pool Logs

```powershell
Get-EventLog -LogName Application -Source "IIS*" -Newest 50
```

### Check Website Bindings

```powershell
Get-WebBinding -Name "PMS"
```

### Restart Application Pool

```powershell
Restart-WebAppPool -Name "PMSAppPool"
```

### Restart Website

```powershell
Restart-Website -Name "PMS"
```

## Updating Deployment

To update the application:

1. Stop the application pool and website
2. Run the publish command again
3. Start the application pool and website

```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name "PMSAppPool"
Stop-Website -Name "PMS"

cd D:\PMS_Coditium
dotnet publish --configuration Release --output "D:\PMSDeploy" --self-contained false

Start-WebAppPool -Name "PMSAppPool"
Start-Website -Name "PMS"
```

## Configuration Files

- **Connection String**: Edit `D:\PMSDeploy\appsettings.json` if needed
- **Logging**: Configured in `appsettings.json`

## Security Considerations

1. Ensure the application pool identity has appropriate permissions
2. Configure firewall rules for port 84
3. Use HTTPS in production (requires SSL certificate)
4. Review and secure connection strings

## Notes

- The application uses .NET 9.0, so ensure the .NET 9.0 Hosting Bundle is installed
- The application pool runs without managed code (for .NET Core/5+)
- Default binding: IP `172.20.228.2`, Port `84`
- Physical path: `D:\PMSDeploy`
