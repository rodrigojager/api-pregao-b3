

Amazon S3 Bucket:
Nome: b3-techchallenge-rj-2025
Arn: arn:aws:s3:::b3-techchallenge-rj-2025
Região: sa-east-1
Event notifications: b3-parquet-trigger -> b3-pipeline-lambda (lambda_job_glue_.py)
Objects: raw, refined, athena-results

Lambda Function:
Nome: b3-pipeline-lambda
Role Name: b3-pipeline-lambda-role-6bp6ymld (acesso AWS Glue através da permissão AllowStartJobRun)
Arn: arn:aws:glue:sa-east-1:726856122495:job/Operacoes_Parquet 
Allow: glue:StartJobRun

AWS Glue Studio Visual ETL Job:
Nome: Operacoes_Parquet.py

Amazon Athena:
Database: b3_techchallenge
Table: pregao_refinado
Result location: s3://b3-techchallenge-rj-2025/athena-results/