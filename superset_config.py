import os

POSTGRES_USER = os.getenv("POSTGRES_USER", "postgres")
POSTGRES_PASSWORD = os.getenv("POSTGRES_PASSWORD", "postgres123")
POSTGRES_HOST = os.getenv("POSTGRES_HOST", "postgres")
POSTGRES_PORT = os.getenv("POSTGRES_PORT", "5432")
POSTGRES_DB = os.getenv("POSTGRES_DB", "fieldservice")
POSTGRES_SCHEMA = os.getenv("POSTGRES_SCHEMA", "superset")

# Conecta no mesmo banco (fieldservice) usando o schema dedicado do Superset.
SQLALCHEMY_DATABASE_URI = (
    f"postgresql://{POSTGRES_USER}:{POSTGRES_PASSWORD}@"
    f"{POSTGRES_HOST}:{POSTGRES_PORT}/{POSTGRES_DB}"
    f"?options=-csearch_path%3D{POSTGRES_SCHEMA}"
)

# Configuração do Cache via seu Redis
REDIS_PASSWORD = os.getenv("REDIS_PASSWORD", "redis123")
REDIS_HOST = os.getenv("REDIS_HOST", "redis")
REDIS_PORT = os.getenv("REDIS_PORT", "6379")

CACHE_CONFIG = {
    "CACHE_TYPE": "RedisCache",
    "CACHE_REDIS_URL": f"redis://:{REDIS_PASSWORD}@{REDIS_HOST}:{REDIS_PORT}/0",
}
DATA_CACHE_CONFIG = CACHE_CONFIG

# Chave Secreta de Sessão
SECRET_KEY = os.getenv("SUPERSET_SECRET_KEY", "SUA_CHAVE_GERADA_AQUI")

# Habilita Embedded BI para integração C#
FEATURE_FLAGS = {
    "EMBEDDED_SUPERSET": True,
    "ENABLE_TEMPLATE_PROCESSING": True,
    
}

# Permissões CORS para acesso pelo Front-End / C#
ENABLE_CORS = True
CORS_OPTIONS = {
    "supports_credentials": True,
    "allow_headers": ["*"],
    "resources": ["*"],
    "origins": ["*"],
}

TALISMAN_CONFIG = {
    "content_security_policy": None,
    "force_https": False,
}