import json
import boto3
import logging
from datetime import datetime
from typing import Dict, Any

# Configurar logging
logger = logging.getLogger()
logger.setLevel(logging.INFO)

# Inicializar clientes AWS
glue_client = boto3.client('glue', region_name='sa-east-1')
s3_client = boto3.client('s3', region_name='sa-east-1')

# Configurações
BUCKET_NAME = 'b3-techchallenge-rj-2025'
GLUE_JOB_NAME = 'Operacoes_Parquet'
REGION = 'sa-east-1'

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    Handler principal da Lambda Function
    
    Args:
        event: Evento do S3 com informações do arquivo
        context: Contexto da execução Lambda
    
    Returns:
        Dict com resultado da execução
    """
    
    try:
        logger.info("=== INICIANDO LAMBDA FUNCTION ===")
        logger.info(f"Evento recebido: {json.dumps(event, indent=2)}")
        
        # 1. EXTRAIR INFORMAÇÕES DO EVENTO S3
        s3_info = extract_s3_info(event)
        if not s3_info:
            return create_response(False, "Falha ao extrair informações do S3")
        
        logger.info(f"Arquivo detectado: {s3_info['bucket']}/{s3_info['key']}")
        
        # 2. VALIDAR SE É UM ARQUIVO PARQUET NA PASTA RAW
        if not is_valid_parquet_file(s3_info['key']):
            logger.info(f"Arquivo ignorado (não é Parquet na pasta raw): {s3_info['key']}")
            return create_response(True, "Arquivo ignorado - não é Parquet na pasta raw")
        
        # 3. INICIAR JOB GLUE
        job_run_id = start_glue_job(s3_info)
        if not job_run_id:
            return create_response(False, "Falha ao iniciar job Glue")
        
        # 4. LOGAR SUCESSO
        logger.info(f"Job Glue iniciado com sucesso! Run ID: {job_run_id}")
        
        return create_response(
            success=True,
            message="Pipeline executado com sucesso",
            data={
                "s3_bucket": s3_info['bucket'],
                "s3_key": s3_info['key'],
                "glue_job_name": GLUE_JOB_NAME,
                "glue_run_id": job_run_id,
                "timestamp": datetime.now().isoformat()
            }
        )
        
    except Exception as e:
        logger.error(f"Erro na execução da Lambda: {str(e)}")
        return create_response(False, f"Erro interno: {str(e)}")

def extract_s3_info(event: Dict[str, Any]) -> Dict[str, str]:
    """
    Extrai informações do arquivo S3 do evento
    
    Args:
        event: Evento do S3
    
    Returns:
        Dict com bucket e key do arquivo
    """
    try:
        # Verificar se é um evento S3
        if 'Records' not in event:
            logger.error("Evento não contém Records")
            return {}
        
        record = event['Records'][0]
        
        # Verificar se é um evento S3
        if record.get('eventSource') != 'aws:s3':
            logger.error("Evento não é do S3")
            return {}
        
        # Extrair informações do S3
        s3_data = record['s3']
        bucket = s3_data['bucket']['name']
        key = s3_data['object']['key']
        
        # Decodificar URL encoding
        import urllib.parse
        key = urllib.parse.unquote_plus(key)
        
        return {
            'bucket': bucket,
            'key': key
        }
        
    except Exception as e:
        logger.error(f"Erro ao extrair informações S3: {str(e)}")
        return {}

def is_valid_parquet_file(s3_key: str) -> bool:
    """
    Valida se o arquivo é um Parquet válido na pasta raw
    
    Args:
        s3_key: Chave do arquivo no S3
    
    Returns:
        True se for válido, False caso contrário
    """
    # Verificar se está na pasta raw
    if not s3_key.startswith('raw/'):
        return False
    
    # Verificar se termina com .parquet
    if not s3_key.endswith('.parquet'):
        return False
    
    # Verificar se tem estrutura de partição (dt=YYYY-MM-DD)
    if 'dt=' not in s3_key:
        return False
    
    return True

def start_glue_job(s3_info: Dict[str, str]) -> str:
    """
    Inicia o job de ETL no AWS Glue
    
    Args:
        s3_info: Informações do arquivo S3
    
    Returns:
        ID da execução do job Glue
    """
    try:
        logger.info(f"Iniciando job Glue: {GLUE_JOB_NAME}")
        
        # Parâmetros para o job Glue
        job_parameters = {
            '--s3_bucket': s3_info['bucket'],
            '--s3_key': s3_info['key'],
            '--region': REGION,
            '--job-language': 'python',
            '--job-bookmark-option': 'job-bookmark-enable'
        }
        
        # Iniciar job Glue
        response = glue_client.start_job_run(
            JobName=GLUE_JOB_NAME,
            Arguments=job_parameters
        )
        
        job_run_id = response['JobRunId']
        logger.info(f"Job Glue iniciado com Run ID: {job_run_id}")
        
        return job_run_id
        
    except glue_client.exceptions.EntityNotFoundException:
        logger.warning(f"Job Glue '{GLUE_JOB_NAME}' não encontrado.")
        return "job-not-created-yet"
        
    except Exception as e:
        logger.error(f"Erro ao iniciar job Glue: {str(e)}")
        return ""

def create_response(success: bool, message: str, data: Dict[str, Any] = None) -> Dict[str, Any]:
    """
    Cria resposta padronizada da Lambda
    
    Args:
        success: Se a execução foi bem-sucedida
        message: Mensagem de resultado
        data: Dados adicionais
    
    Returns:
        Dict com resposta padronizada
    """
    response = {
        "success": success,
        "message": message,
        "timestamp": datetime.now().isoformat(),
        "region": REGION
    }
    
    if data:
        response["data"] = data
    
    return response 