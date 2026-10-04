using FieldService.Superset.Attributes;

namespace FieldService.Superset.Utils;

public static class SupersetContainerBootstrapScript
{
    public static string BuildBootstrapCommand(
        string metadataSchema,
        string dataSchema,
        string mockedDataSchema,
        string remoteUserHeaderName,
        int containerPort,
        int gunicornWorkers,
        int gunicornTimeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(mockedDataSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteUserHeaderName);

        string EscapePy(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        var restrictedPvmsPython = string.Join(
            Environment.NewLine,
            SupersetRestrictedPermissions.AllTenantRestrictions
                .Select(permission =>
                    $"                (\"{EscapePy(permission.Permission)}\", \"{EscapePy(permission.ViewMenu)}\"),"));

        return $$"""
        set -eu

        cat <<'EOF' >/app/pythonpath/superset_config.py
        import os
        from flask import Flask
        from flask_appbuilder.security.manager import AUTH_REMOTE_USER
        from sqlalchemy import event, text

        SECRET_KEY = os.environ["SUPERSET_SECRET_KEY"]
        SQLALCHEMY_DATABASE_URI = os.environ["SUPERSET_METADATA_DATABASE_URL"]
        METADATA_SCHEMA = os.environ.get("SUPERSET_METADATA_SCHEMA", "{{metadataSchema}}")

        # Configuracoes de Banco de Dados
        SQLALCHEMY_ENGINE_OPTIONS = {
            "connect_args": {
                "options": f"-c search_path={METADATA_SCHEMA},public"
            }
        }

        # Autenticacao via Remote User (Impersonation)
        AUTH_TYPE = AUTH_REMOTE_USER
        HTTP_HEADER_REMOTE_USER = os.environ.get("HTTP_HEADER_REMOTE_USER", "{{remoteUserHeaderName}}")

        # Bloqueia criacao automatica de usuarios (Nao cria perfil Gamma sozinho)
        AUTH_USER_REGISTRATION = False

        # Permite incorporacao em Iframe e CORS
        ENABLE_CORS = True
        CORS_OPTIONS = {
            "supports_credentials": True,
            "allow_headers": ["*"],
            "resources": ["*"],
            "origins": ["*"]
        }

        # Leitura dinamica das origens do Frontend para o Iframe
        raw_origins = os.environ.get("ALLOWED_FRAME_ANCESTORS", "").split()

        TALISMAN_CONFIG = {
            "content_security_policy": {
                "frame-ancestors": ["'self'"] + raw_origins
            },
            "force_https": False
        }

        X_FRAME_OPTIONS = "ALLOWALL"

        # Suporte a Cookies em Iframe Cross-Origin
        SESSION_COOKIE_SAMESITE = "None"
        SESSION_COOKIE_SECURE = True

        FEATURE_FLAGS = {
            "EMBEDDED_SUPERSET": True
        }

        # Enforce mandatory tenant dashboard scoping (Tenant_<id>_Scope) for every new dashboard.
        TENANT_SCOPE_ROLE_PREFIX = "Tenant_"
        TENANT_SCOPE_ROLE_SUFFIX = "_Scope"

        def _register_tenant_dashboard_scope_listeners(app: Flask) -> None:
            if app.extensions.get("tenant_dashboard_scope_listeners_registered"):
                return

            with app.app_context():
                from superset.models.dashboard import Dashboard

                def _resolve_single_tenant_scope_role_id(connection, user_id):
                    rows = connection.execute(
                        text(
                            "SELECT r.id "
                            "FROM ab_user_role ur "
                            "JOIN ab_role r ON r.id = ur.role_id "
                            "WHERE ur.user_id = :user_id "
                            "AND r.name LIKE :role_pattern"
                        ),
                        {
                            "user_id": user_id,
                            "role_pattern": f"{TENANT_SCOPE_ROLE_PREFIX}%{TENANT_SCOPE_ROLE_SUFFIX}",
                        },
                    ).fetchall()

                    if len(rows) != 1:
                        raise RuntimeError(
                            "Dashboard creation blocked: creator must have exactly one Tenant_*_Scope role."
                        )

                    return int(rows[0][0])

                @event.listens_for(Dashboard, "before_insert")
                def _enforce_tenant_scope_before_dashboard_insert(mapper, connection, target):
                    creator_id = getattr(target, "created_by_fk", None)
                    if creator_id is None:
                        raise RuntimeError(
                            "Dashboard creation blocked: created_by_fk is required for tenant scope resolution."
                        )

                    role_id = _resolve_single_tenant_scope_role_id(connection, creator_id)
                    setattr(target, "_tenant_scope_role_id", role_id)

                @event.listens_for(Dashboard, "after_insert")
                def _attach_tenant_scope_after_dashboard_insert(mapper, connection, target):
                    role_id = getattr(target, "_tenant_scope_role_id", None)
                    if role_id is None:
                        creator_id = getattr(target, "created_by_fk", None)
                        role_id = _resolve_single_tenant_scope_role_id(connection, creator_id)

                    connection.execute(
                        text(
                            "INSERT INTO dashboard_roles (dashboard_id, role_id) "
                            "SELECT :dashboard_id, :role_id "
                            "WHERE NOT EXISTS ("
                            "SELECT 1 FROM dashboard_roles "
                            "WHERE dashboard_id = :dashboard_id AND role_id = :role_id"
                            ")"
                        ),
                        {"dashboard_id": target.id, "role_id": role_id},
                    )

            app.extensions["tenant_dashboard_scope_listeners_registered"] = True

        def FLASK_APP_MUTATOR(app: Flask) -> None:
            _register_tenant_dashboard_scope_listeners(app)
        EOF

        superset db upgrade

        # Garante a criacao ou atualizacao do usuario Admin sem silenciar erros criticos
        superset fab create-admin \
          --username "$SUPERSET_ADMIN_USERNAME" \
          --firstname "$SUPERSET_ADMIN_FIRST_NAME" \
          --lastname "$SUPERSET_ADMIN_LAST_NAME" \
          --email "$SUPERSET_ADMIN_EMAIL" \
          --password "$SUPERSET_ADMIN_PASSWORD" || true

        superset init

        # Script Python inline para expurgar permissoes globais e criar os escopos de Schema
        python3 - <<'PYTHON_SCRIPT'
        import os
        from superset.app import create_app
        from superset.extensions import db, security_manager

        app = create_app()
        with app.app_context():
            from superset.models.core import Database

            db_name = os.environ.get("SUPERSET_TENANT_DB_NAME")
            if not db_name:
                raise RuntimeError("SUPERSET_TENANT_DB_NAME is required for tenant role bootstrap.")

            tenant_id_raw = os.environ.get("TENANT_ID")
            if not tenant_id_raw:
                raise RuntimeError("TENANT_ID is required for tenant role bootstrap.")

            tenant_id_n = tenant_id_raw.replace("-", "").lower()
            tenant_scope_role_name = f"Tenant_{tenant_id_n}_Scope"

            database_uri = os.environ.get("DATABASE_URL") or os.environ.get("SUPERSET_METADATA_DATABASE_URL")
            if not database_uri:
                raise RuntimeError("DATABASE_URL or SUPERSET_METADATA_DATABASE_URL is required for tenant role bootstrap.")

            schema_scopes = {
                os.environ.get("SUPERSET_DATA_SCHEMA_ROLE_NAME", "Scope_Data"): os.environ.get("SUPERSET_DATA_SCHEMA", "{{dataSchema}}"),
                os.environ.get("SUPERSET_MOCKED_SCHEMA_ROLE_NAME", "Scope_MockedData"): os.environ.get("SUPERSET_MOCKED_DATA_SCHEMA", "{{mockedDataSchema}}")
            }

            database = db.session.query(Database).filter_by(database_name=db_name).one_or_none()
            if database is None:
                database = Database(database_name=db_name, expose_in_sqllab=True)
                db.session.add(database)

            database.database_name = db_name
            database.expose_in_sqllab = True
            database.set_sqlalchemy_uri(database_uri)
            db.session.commit()

            security_manager.create_missing_perms()

            database_perm = security_manager.get_database_perm(database.id, database.database_name)
            if database_perm:
                security_manager.add_permission_view_menu("database_access", database_perm)

            db_pvm = security_manager.find_permission_view_menu("database_access", database_perm) if database_perm else None

            # 0. Garante a role de escopo do tenant usada para isolamento de dashboards.
            tenant_scope_role = security_manager.find_role(tenant_scope_role_name)
            if not tenant_scope_role:
                tenant_scope_role = security_manager.add_role(tenant_scope_role_name)

            if db_pvm and db_pvm not in tenant_scope_role.permissions:
                tenant_scope_role.permissions.append(db_pvm)

            schema_permission_names = {}
            for schema_name in schema_scopes.values():
                schema_perm = security_manager.get_schema_perm(database, schema_name)
                schema_permission_names[schema_name] = schema_perm
                if schema_perm:
                    security_manager.add_permission_view_menu("schema_access", schema_perm)

            db.session.commit()

            # 1. Permissoes Globais a REVOGAR dos perfis padrao (Alpha, sql_lab, Gamma)
            restricted_pvms = [
            {{restrictedPvmsPython}}
            ]

            roles_to_clean = ["Alpha", "sql_lab", "Gamma"]

            for r_name in roles_to_clean:
                role = security_manager.find_role(r_name)
                if not role:
                    continue

                for action, view in restricted_pvms:
                    pvm = security_manager.find_permission_view_menu(action, view)
                    if pvm and pvm in role.permissions:
                        role.permissions.remove(pvm)

                if db_pvm and db_pvm in role.permissions:
                    role.permissions.remove(db_pvm)

                global_access_pvms = [
                    ("all_database_access", "all_database_access"),
                    ("all database access", "all_database_access"),
                    ("all_datasource_access", "all_datasource_access"),
                    ("all datasource access", "all_datasource_access"),
                ]
                for action, view in global_access_pvms:
                    wildcard_pvm = security_manager.find_permission_view_menu(action, view)
                    if wildcard_pvm and wildcard_pvm in role.permissions:
                        role.permissions.remove(wildcard_pvm)

                if r_name in {"Alpha", "Gamma"}:
                    all_query_pvm = security_manager.find_permission_view_menu("can_access", "all_query_access")
                    if all_query_pvm and all_query_pvm in role.permissions:
                        role.permissions.remove(all_query_pvm)

            # 2. Criacao das Roles de Escopo de Schema para dados reais e dados mocked.
            for role_name, schema_name in schema_scopes.items():
                scope_role = security_manager.find_role(role_name)
                if not scope_role:
                    scope_role = security_manager.add_role(role_name)

                schema_perm = schema_permission_names.get(schema_name)
                schema_pvm = security_manager.find_permission_view_menu("schema_access", schema_perm) if schema_perm else None
                if schema_pvm and schema_pvm not in scope_role.permissions:
                    scope_role.permissions.append(schema_pvm)

            db.session.commit()
            print(">>> Bootstrapping de permissoes do Tenant finalizado com sucesso!")
        PYTHON_SCRIPT

        exec gunicorn \
          --bind "0.0.0.0:{{containerPort}}" \
          --workers "{{gunicornWorkers}}" \
          --timeout "{{gunicornTimeoutSeconds}}" \
          "superset.app:create_app()"
        """;
    }
}

