import sys
from awsglue.transforms import *
from awsglue.utils import getResolvedOptions
from pyspark.context import SparkContext
from awsglue.context import GlueContext
from awsglue.job import Job
from awsgluedq.transforms import EvaluateDataQuality
from awsglue.dynamicframe import DynamicFrame
import gs_derived
from pyspark.sql import functions as SqlFuncs

def sparkAggregate(glueContext, parentFrame, groups, aggs, transformation_ctx) -> DynamicFrame:
    aggsFuncs = []
    for column, func in aggs:
        aggsFuncs.append(getattr(SqlFuncs, func)(column))
    result = parentFrame.toDF().groupBy(*groups).agg(*aggsFuncs) if len(groups) > 0 else parentFrame.toDF().agg(*aggsFuncs)
    return DynamicFrame.fromDF(result, glueContext, transformation_ctx)

args = getResolvedOptions(sys.argv, ['JOB_NAME'])
sc = SparkContext()
glueContext = GlueContext(sc)
spark = glueContext.spark_session
job = Job(glueContext)
job.init(args['JOB_NAME'], args)

# Default ruleset used by all target nodes with data quality enabled
DEFAULT_DATA_QUALITY_RULESET = """
    Rules = [
        ColumnCount > 0
    ]
"""

# Script generated for node Parkets from S3 Bucket
ParketsfromS3Bucket_node1752994291791 = glueContext.create_dynamic_frame.from_options(format_options={}, connection_type="s3", format="parquet", connection_options={"paths": ["s3://b3-techchallenge-rj-2025/raw/"], "recurse": True}, transformation_ctx="ParketsfromS3Bucket_node1752994291791")

# Script generated for node Rename Columns
RenameColumns_node1753042735861 = ApplyMapping.apply(frame=ParketsfromS3Bucket_node1752994291791, mappings=[("datapregao", "date", "datapregao", "date"), ("ticker", "string", "codigo_acao", "string"), ("preco", "decimal", "valor", "decimal"), ("quantidade", "long", "quantidade", "long")], transformation_ctx="RenameColumns_node1753042735861")

# Script generated for node Derived Column by Expression
DerivedColumnbyExpression_node1752996510111 = RenameColumns_node1753042735861.gs_derived(colName="ano_pregao", expr="year(DataPregao)")

# Script generated for node Aggregation of data
Aggregationofdata_node1752997383850 = sparkAggregate(glueContext, parentFrame = DerivedColumnbyExpression_node1752996510111, groups = ["codigo_acao"], aggs = [["valor", "avg"], ["valor", "max"], ["valor", "min"], ["quantidade", "sum"], ["quantidade", "count"], ["datapregao", "min"], ["datapregao", "max"]], transformation_ctx = "Aggregationofdata_node1752997383850")

# Script generated for node Rename Columns
RenameColumns_node1753043824889 = ApplyMapping.apply(frame=Aggregationofdata_node1752997383850, mappings=[("codigo_acao", "string", "codigo_acao", "string"), ("`avg(valor)`", "double", "valor_medio", "decimal"), ("`max(valor)`", "decimal", "valor_maximo", "decimal"), ("`min(valor)`", "decimal", "valor_minimo", "decimal"), ("`sum(quantidade)`", "long", "`sum(quantidade)`", "long"), ("`count(quantidade)`", "long", "quantidade_amostras", "long"), ("`min(datapregao)`", "date", "data_min", "date"), ("`max(datapregao)`", "date", "data_max", "date")], transformation_ctx="RenameColumns_node1753043824889")

# Script generated for node Date Subtraction
DateSubtraction_node1753031250827 = RenameColumns_node1753043824889.gs_derived(colName="periodo_dias", expr="datediff(to_date(data_max), to_date(data_min))")

# Script generated for node Date of Partition
DateofPartition_node1753045604053 = DateSubtraction_node1753031250827.gs_derived(colName="data_particao", expr="date_format(data_max, 'yyyy-MM-dd')")

# Script generated for node S3OutputRefined
EvaluateDataQuality().process_rows(frame=DateofPartition_node1753045604053, ruleset=DEFAULT_DATA_QUALITY_RULESET, publishing_options={"dataQualityEvaluationContext": "EvaluateDataQuality_node1753030765906", "enableDataQualityResultsPublishing": True}, additional_options={"dataQualityResultsPublishing.strategy": "BEST_EFFORT", "observations.scope": "ALL"})
S3OutputRefined_node1753045417481 = glueContext.getSink(path="s3://b3-techchallenge-rj-2025/refined/", connection_type="s3", updateBehavior="UPDATE_IN_DATABASE", partitionKeys=["codigo_acao", "data_particao"], enableUpdateCatalog=True, transformation_ctx="S3OutputRefined_node1753045417481")
S3OutputRefined_node1753045417481.setCatalogInfo(catalogDatabase="b3_techchallenge",catalogTableName="pregao_refinado")
S3OutputRefined_node1753045417481.setFormat("glueparquet", compression="snappy")
S3OutputRefined_node1753045417481.writeFrame(DateofPartition_node1753045604053)
job.commit()