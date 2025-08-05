# 🚀 TechChallenge - Deploy Elastic Beanstalk

## 📋 Status Atual

✅ **Aplicação funcionando localmente**  
✅ **Build e publish funcionando**  
✅ **Bundle de deploy criado**  
⚠️ **Playwright não funcionará no Elastic Beanstalk** (esperado)  
✅ **Todos os outros recursos funcionando**  

## 🎯 Deploy na AWS

### **1. Preparar Deploy**

```powershell
# Executar script de preparação
.\prepare-eb-deploy.ps1
```

### **2. Upload no Elastic Beanstalk**

1. **Acesse**: [AWS Elastic Beanstalk Console](https://console.aws.amazon.com/elasticbeanstalk/)
2. **Crie nova aplicação**:
   - **Application name**: `TechChallenge`
   - **Platform**: `.NET on Linux`
   - **Platform branch**: `.NET 9 running on 64bit Amazon Linux 2`
3. **Upload do arquivo**: `eb-deploy-bundle.zip`

### **3. Configurar Variáveis de Ambiente**

No console do Elastic Beanstalk → Configuration → Software:

```
AWS_ACCESS_KEY_ID=sua-access-key
AWS_SECRET_ACCESS_KEY=sua-secret-key
AWS_DEFAULT_REGION=sa-east-1
Aws__Region=sa-east-1
Aws__Bucket=b3-techchallenge-rj-2025
Parquet__TempFolder=/var/app/temp
```

## 🎯 Endpoints Disponíveis

- **🌐 Aplicação**: `http://seu-ambiente.elasticbeanstalk.com`
- **📚 Swagger**: `http://seu-ambiente.elasticbeanstalk.com/swagger`
- **❤️ Health**: `http://seu-ambiente.elasticbeanstalk.com/health`
- **📄 Docs**: `http://seu-ambiente.elasticbeanstalk.com/docs`

## ⚠️ Limitações Conhecidas

- **Playwright**: Não funcionará no Elastic Beanstalk (limitação da plataforma)
- **Outros recursos**: Todos funcionando normalmente

## 💰 Custos Estimados

- **t3.small**: ~$15-20/mês
- **t3.micro**: ~$8-12/mês

## 🛠️ Comandos Úteis

```powershell
# Preparar deploy
.\prepare-eb-deploy.ps1

# Limpar arquivos temporários
.\cleanup-temp.ps1

# Build local
dotnet build -c Release

# Run local
dotnet run
```

## 📁 Arquivos Importantes

- **`eb-deploy-bundle.zip`**: Bundle para upload na AWS
- **`.ebextensions/00_unified_config.config`**: Configuração do Elastic Beanstalk
- **`prepare-eb-deploy.ps1`**: Script de preparação
- **`DEPLOY-INSTRUCTIONS.md`**: Guia detalhado de deploy

---

**🎉 Aplicação pronta para deploy na AWS!**