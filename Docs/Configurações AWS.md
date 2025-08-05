# Configurações da AWS

Para consulta e referência, algumas configurações utilizadas na AWS estão documentadas abaixo. Os arquivos com códigos estão disponíveis no GitHub do desenvolvedor.

---

### AWS S3 Bucket

Foi criado um bucket no **AWS S3** para armazenar os arquivos Parquet. Dentro de uma pasta chamada `raw`, há um *trigger* (`b3-parquet-trigger`) que, ao detectar a chegada de um novo arquivo, dispara uma função Lambda (`b3-pipeline-lambda`) responsável por iniciar um job ETL no AWS Glue.

- **Nome:** `b3-techchallenge-rj-2025`  
- **ARN:** `arn:aws:s3:::b3-techchallenge-rj-2025`  
- **Região:** `sa-east-1`  
- **Notificação de eventos:** `b3-parquet-trigger` → `b3-pipeline-lambda` (`lambda_job_glue.py`)  
- **Pastas:** `raw/`, `refined/`, `athena-results/`

---

### AWS Lambda Functions

Foram criadas duas funções **AWS Lambda** em Python:

#### 1. `b3-pipeline-lambda`

Responsável por iniciar o job ETL no Glue.

- **Script:** `AWS/lambda_job_glue.py` (disponível no GitHub)  
- **Role:** `b3-pipeline-lambda-role-6bp6ymld`  
- **Permissão:** `glue:StartJobRun`  
- **Glue Job ARN:**  
  `arn:aws:glue:sa-east-1:726856122495:job/Operacoes_Parquet`

Código do script:

#### 2. `DailyPipelineTrigger`

Disparada por um cronjob configurado no EventBridge (`DailyTrigger`) para executar o pipeline completo via API.

- **Script:** `AWS/lambda_pipeline_trigger.py` (disponível no GitHub)  
- **Role:** `DailyPipelineTrigger-role-nkv39juy`  
- **Permissões:** `logs:CreateLogStream`, `logs:PutLogEvents`  
- **Lambda ARN:**  
  `arn:aws:lambda:sa-east-1:726856122495:function:DailyPipelineTrigger`

---

### AWS Glue Studio (ETL Visual)

Embora o job ETL do Glue tenha sido criado visualmente no **AWS Glue Studio**, o script Python correspondente foi exportado e está disponível em:  
`AWS/Operacoes_Parquet.py` (GitHub)

---

### AWS Athena

Foi criado um banco de dados com uma tabela no Glue Catalog. Os dados podem ser consultados diretamente pelo **AWS Athena**.

- **Database:** `b3_techchallenge`  
- **Table:** `pregao_refinado`  
- **Resultado:** `s3://b3-techchallenge-rj-2025/athena-results/`
