import json
import urllib3

http = urllib3.PoolManager()

def lambda_handler(event, context):
    url = "http://tech-challenge-rodrigo.sa-east-1.elasticbeanstalk.com/api/ExecutePipeline"

    try:
        response = http.request(
            "POST",
            url,
            body=json.dumps({}),
            headers={"Content-Type": "application/json"}
        )

        return {
            "statusCode": response.status,
            "body": response.data.decode("utf-8")
        }

    except Exception as e:
        return {
            "statusCode": 500,
            "body": f"Erro ao chamar o endpoint: {str(e)}"
        }
