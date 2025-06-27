# PoFastType Infrastructure Documentation

This directory contains the Infrastructure as Code (IaC) templates for deploying the PoFastType application to Azure using Bicep.

## Architecture

The infrastructure follows Azure best practices and includes:

- **App Service Plan**: Scalable hosting plan for the web application
- **App Service (Web App)**: Hosts the PoFastType ASP.NET Core application
- **Storage Account**: Provides Azure Table Storage for game results
- **Staging Slot**: Production environment includes a staging slot for safe deployments

## Design Patterns Used

- **Strategy Pattern**: Applied in naming conventions for consistent resource naming
- **Factory Pattern**: Bicep templates act as resource factories with parameterized configuration
- **Template Method Pattern**: Common deployment steps with environment-specific variations

## Security Features

✅ **HTTPS Only**: All web traffic encrypted in transit
✅ **TLS 1.2 Minimum**: Modern encryption standards enforced
✅ **Managed Identity**: Secure service-to-service authentication
✅ **Storage Encryption**: Data encrypted at rest
✅ **FTP Disabled**: Prevents insecure file transfer protocols
✅ **Public Blob Access Disabled**: Prevents unauthorized data access

## Files

- `main.bicep`: Main infrastructure template
- `main.parameters.json`: Production environment parameters
- `main.staging.parameters.json`: Staging environment parameters

## Deployment

### Automated Deployment (Recommended)

The GitHub Actions workflow (`.github/workflows/deploy.yml`) automatically deploys infrastructure and application:

1. **Push to main/master**: Deploys to production
2. **Pull Request**: Deploys to staging environment
3. **Manual trigger**: Can be triggered via GitHub Actions UI

### Manual Deployment

#### Prerequisites

- Azure CLI installed and authenticated
- Appropriate Azure permissions (Contributor role)

#### Commands

```powershell
# Deploy to production
az deployment group create `
  --resource-group rg-pofasttype-prod `
  --template-file main.bicep `
  --parameters @main.parameters.json

# Deploy to staging
az deployment group create `
  --resource-group rg-pofasttype-staging `
  --template-file main.bicep `
  --parameters @main.staging.parameters.json

# Preview changes (What-If)
az deployment group what-if `
  --resource-group rg-pofasttype-prod `
  --template-file main.bicep `
  --parameters @main.parameters.json
```

## Environment Configuration

### Production Environment
- **Resource Group**: `rg-pofasttype-prod`
- **App Service Plan**: `B1` (Basic tier with Always On)
- **Storage**: `Standard_LRS` (Locally redundant storage)
- **Staging Slot**: Enabled for blue-green deployments

### Staging Environment
- **Resource Group**: `rg-pofasttype-staging`
- **App Service Plan**: `F1` (Free tier for cost optimization)
- **Storage**: `Standard_LRS`
- **Staging Slot**: Disabled

## Application Settings

The following application settings are automatically configured:

| Setting | Description |
|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Production/Staging/Development) |
| `AzureTableStorage__ConnectionString` | Secure connection to Table Storage |
| `AzureTableStorage__TableName` | Table name (environment-specific) |
| `Logging__LogLevel__Default` | Logging level configuration |

## Monitoring and Health Checks

- **Health Check Endpoint**: `/api/diag/health`
- **Application Insights**: Can be added via additional Bicep module
- **Log Streaming**: Available via Azure Portal or CLI

## Scaling Considerations

The infrastructure supports horizontal scaling:

- **App Service Plan**: Can be scaled up/out based on demand
- **Table Storage**: Automatically scales based on usage
- **CDN**: Can be added for global content delivery

## Cost Optimization

- **Free Tier**: Staging uses F1 (free) App Service Plan
- **Right-sizing**: Production uses B1 for cost-effective baseline
- **Storage Tier**: Hot tier optimized for frequent access

## Security Compliance

The infrastructure follows these security standards:

- **Azure Security Baseline**: Implements recommended security controls
- **HTTPS Everywhere**: All communication encrypted
- **Least Privilege**: Minimal required permissions
- **Defense in Depth**: Multiple security layers

## Troubleshooting

### Common Issues

1. **Deployment Failures**: Check resource group permissions
2. **Storage Access**: Verify connection string configuration
3. **App Service Issues**: Review application logs and health checks

### Useful Commands

```powershell
# View deployment history
az deployment group list --resource-group rg-pofasttype-prod

# Get deployment outputs
az deployment group show --name <deployment-name> --resource-group rg-pofasttype-prod --query properties.outputs

# Stream application logs
az webapp log tail --name <app-name> --resource-group rg-pofasttype-prod
```

## Future Enhancements

Potential infrastructure improvements:

- **Application Insights**: Add comprehensive monitoring
- **Azure CDN**: Global content delivery
- **Key Vault**: External secret management
- **Auto-scaling**: Dynamic scaling based on metrics
- **VNet Integration**: Enhanced network security
- **Private Endpoints**: Private network connectivity
