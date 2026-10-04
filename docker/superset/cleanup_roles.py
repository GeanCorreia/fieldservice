#!/usr/bin/env python3
"""Clean restricted permissions from selected Superset roles.

This script is intended to run after `superset init` in the container bootstrap.
It removes selected database/dataset permissions from Alpha, sql_lab and Gamma
for the configured Superset database name.
"""

from __future__ import annotations

import os

from superset.app import create_app
from superset.extensions import db, security_manager


RESTRICTED_PVMS = [
    ("can_read", "Database"),
    ("can_write", "Database"),
    ("can_add", "Database"),
    ("can_delete", "Database"),
    ("can_export", "Database"),
    ("can_external_metadata_by_name", "Database"),
    ("can_select_star", "Database"),
    ("can_external_metadata", "Datasource"),
    ("can_external_metadata_by_name", "Datasource"),
    ("can_get_or_create_dataset", "Dataset"),
    ("can_save", "Datasource"),
    ("can_write", "Dataset"),
    ("can_add", "Dataset"),
    ("can_delete", "Dataset"),
    ("can_refresh", "Dataset"),
]

ROLES_TO_CLEAN = ["Alpha", "sql_lab", "Gamma"]


def main() -> int:
    app = create_app()
    with app.app_context():
        target_db_name = os.environ.get("SUPERSET_APP_DB_NAME", "PostgreSQL")


        cleaned_roles = []
        for role_name in ROLES_TO_CLEAN:
            role = security_manager.find_role(role_name)
            if not role:
                continue

            role_changed = False

            # Expurga permissões de conexão e gestão de infraestrutura.
            for action, view in RESTRICTED_PVMS:
                pvm = security_manager.find_permission_view_menu(action, view)
                if pvm and pvm in role.permissions:
                    role.permissions.remove(pvm)
                    role_changed = True

            # Remove o acesso irrestrito ao banco inteiro para forçar a checagem por Schema.
            db_pvm = security_manager.find_permission_view_menu("database_access", f"[{target_db_name}]")
            if db_pvm and db_pvm in role.permissions:
                role.permissions.remove(db_pvm)
                role_changed = True

            if role_changed:
                cleaned_roles.append(role_name)

        db.session.commit()

        if cleaned_roles:
            print(f"Restricted permissions removed from roles: {', '.join(cleaned_roles)}")
        else:
            print("No restricted permissions were removed.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())



