using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScaleCatalog : Migration
    {
        private const string BackfillSql = """
            UPDATE substances SET natural_key = 'legacy:' || id::text WHERE natural_key = '';
            UPDATE reactions SET natural_key = 'legacy:' || id::text WHERE natural_key = '';
            UPDATE catalog_releases SET status = 'Dropped' WHERE status = '';
            """;

        private const string ChangeLogSql = """
            CREATE TABLE catalog_changes (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                table_name text NOT NULL,
                changed_at timestamp with time zone NOT NULL DEFAULT now()
            );

            CREATE FUNCTION log_catalog_rows() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF EXISTS (SELECT 1 FROM changed_rows) THEN
                    INSERT INTO catalog_changes (table_name) VALUES (TG_TABLE_NAME);
                END IF;

                RETURN NULL;
            END;
            $$;

            CREATE FUNCTION log_catalog_truncate() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                INSERT INTO catalog_changes (table_name) VALUES (TG_TABLE_NAME);

                RETURN NULL;
            END;
            $$;

            CREATE TRIGGER logs_inserts AFTER INSERT ON elements
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_updates AFTER UPDATE ON elements
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_deletes AFTER DELETE ON elements
                REFERENCING OLD TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_truncates AFTER TRUNCATE ON elements
                FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_truncate();

            CREATE TRIGGER logs_inserts AFTER INSERT ON substances
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_updates AFTER UPDATE ON substances
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_deletes AFTER DELETE ON substances
                REFERENCING OLD TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_truncates AFTER TRUNCATE ON substances
                FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_truncate();

            CREATE TRIGGER logs_inserts AFTER INSERT ON reactions
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_updates AFTER UPDATE ON reactions
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_deletes AFTER DELETE ON reactions
                REFERENCING OLD TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_truncates AFTER TRUNCATE ON reactions
                FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_truncate();

            CREATE TRIGGER logs_inserts AFTER INSERT ON reaction_participants
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_updates AFTER UPDATE ON reaction_participants
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_deletes AFTER DELETE ON reaction_participants
                REFERENCING OLD TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_truncates AFTER TRUNCATE ON reaction_participants
                FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_truncate();

            CREATE TRIGGER logs_inserts AFTER INSERT ON chemistry_reference
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_updates AFTER UPDATE ON chemistry_reference
                REFERENCING NEW TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_deletes AFTER DELETE ON chemistry_reference
                REFERENCING OLD TABLE AS changed_rows FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_rows();
            CREATE TRIGGER logs_truncates AFTER TRUNCATE ON chemistry_reference
                FOR EACH STATEMENT EXECUTE FUNCTION log_catalog_truncate();

            CREATE TABLE catalog_imports (
                build_id character varying(100) PRIMARY KEY,
                content_hash character varying(64) NOT NULL,
                imported_at timestamp with time zone NOT NULL,
                substances integer NOT NULL,
                reactions integer NOT NULL,
                retired_substances integer NOT NULL,
                retired_reactions integer NOT NULL
            );

            CREATE TABLE catalog_instances (
                instance_id uuid PRIMARY KEY,
                held_releases uuid[] NOT NULL,
                seen_at timestamp with time zone NOT NULL
            );
            """;

        private const string ReleaseTablesSql = """
            CREATE TABLE release_substances (
                release_id uuid NOT NULL,
                id uuid NOT NULL,
                formula character varying(100) NOT NULL,
                kind character varying(20) NOT NULL,
                is_organic boolean NOT NULL,
                state_at_room_temperature character varying(20) NOT NULL,
                weight numeric(12,5),
                structure jsonb,
                category character varying(100),
                translations jsonb NOT NULL,
                retired_at timestamp with time zone,
                prominence integer NOT NULL,
                structure_key character varying(32),
                PRIMARY KEY (release_id, id)
            ) PARTITION BY LIST (release_id);

            CREATE INDEX ix_release_substances_structure_key ON release_substances (release_id, structure_key);
            CREATE INDEX ix_release_substances_prominence ON release_substances (release_id, prominence DESC, id);

            CREATE TABLE release_substance_names (
                release_id uuid NOT NULL,
                locale character varying(10) NOT NULL,
                substance_id uuid NOT NULL,
                name_key text COLLATE "C" NOT NULL,
                prominence integer NOT NULL
            ) PARTITION BY LIST (release_id);

            CREATE INDEX ix_release_substance_names_key ON release_substance_names (release_id, locale, name_key);

            CREATE TABLE release_prefix_tops (
                release_id uuid NOT NULL,
                locale character varying(10) NOT NULL,
                prefix text COLLATE "C" NOT NULL,
                rank smallint NOT NULL,
                substance_id uuid NOT NULL,
                prominence integer NOT NULL,
                PRIMARY KEY (release_id, locale, prefix, rank)
            ) PARTITION BY LIST (release_id);

            CREATE EXTENSION IF NOT EXISTS btree_gin;

            CREATE TABLE release_words (
                release_id uuid NOT NULL,
                locale character varying(10) NOT NULL,
                word text COLLATE "C" NOT NULL,
                prominence integer NOT NULL,
                PRIMARY KEY (release_id, locale, word)
            ) PARTITION BY LIST (release_id);

            CREATE INDEX ix_release_words_trigrams ON release_words USING gin (locale, word gin_trgm_ops);

            CREATE TABLE release_substance_elements (
                release_id uuid NOT NULL,
                element character varying(3) NOT NULL,
                prominence integer NOT NULL,
                substance_id uuid NOT NULL,
                PRIMARY KEY (release_id, element, substance_id)
            ) PARTITION BY LIST (release_id);

            CREATE INDEX ix_release_substance_elements_prominence
                ON release_substance_elements (release_id, element, prominence DESC, substance_id);

            CREATE TABLE release_reactions (
                release_id uuid NOT NULL,
                id uuid NOT NULL,
                type character varying(40) NOT NULL,
                difficulty integer NOT NULL,
                is_reversible boolean NOT NULL,
                retired_at timestamp with time zone,
                enthalpy_kj_per_mol numeric(12,4),
                activation_energy_kj_per_mol numeric(12,4),
                temperature_k numeric(12,4),
                pressure_kpa numeric(12,4),
                catalyst character varying(200),
                effect_preset_key character varying(50),
                animation_duration_ms integer,
                rule character varying(60) NOT NULL,
                confidence numeric(4,3) NOT NULL,
                tags text[] NOT NULL,
                translations jsonb NOT NULL,
                reactant_ids uuid[] NOT NULL,
                rank integer NOT NULL,
                PRIMARY KEY (release_id, id)
            ) PARTITION BY LIST (release_id);

            CREATE TABLE release_participants (
                release_id uuid NOT NULL,
                id uuid NOT NULL,
                reaction_id uuid NOT NULL,
                substance_id uuid NOT NULL,
                role character varying(20) NOT NULL,
                coefficient integer NOT NULL,
                state character varying(20),
                PRIMARY KEY (release_id, id)
            ) PARTITION BY LIST (release_id);

            CREATE INDEX ix_release_participants_reaction ON release_participants (release_id, reaction_id);
            """;

        private const string DatabaseSettingsSql = """
            DO $$
            BEGIN
                EXECUTE format('ALTER DATABASE %I SET pg_trgm.word_similarity_threshold = 0.4', current_database());
                EXECUTE format('ALTER DATABASE %I SET max_parallel_workers_per_gather = 0', current_database());
            END;
            $$;
            """;

        private const string DropSql = """
            DO $$
            BEGIN
                EXECUTE format('ALTER DATABASE %I RESET pg_trgm.word_similarity_threshold', current_database());
                EXECUTE format('ALTER DATABASE %I RESET max_parallel_workers_per_gather', current_database());
            END;
            $$;

            DROP TABLE IF EXISTS release_participants;
            DROP TABLE IF EXISTS release_reactions;
            DROP TABLE IF EXISTS release_substance_elements;
            DROP TABLE IF EXISTS release_words;
            DROP EXTENSION IF EXISTS btree_gin;
            DROP TABLE IF EXISTS release_prefix_tops;
            DROP TABLE IF EXISTS release_substance_names;
            DROP TABLE IF EXISTS release_substances;
            DROP TABLE IF EXISTS catalog_instances;
            DROP TABLE IF EXISTS catalog_imports;
            DROP TRIGGER IF EXISTS logs_inserts ON elements;
            DROP TRIGGER IF EXISTS logs_updates ON elements;
            DROP TRIGGER IF EXISTS logs_deletes ON elements;
            DROP TRIGGER IF EXISTS logs_truncates ON elements;
            DROP TRIGGER IF EXISTS logs_inserts ON substances;
            DROP TRIGGER IF EXISTS logs_updates ON substances;
            DROP TRIGGER IF EXISTS logs_deletes ON substances;
            DROP TRIGGER IF EXISTS logs_truncates ON substances;
            DROP TRIGGER IF EXISTS logs_inserts ON reactions;
            DROP TRIGGER IF EXISTS logs_updates ON reactions;
            DROP TRIGGER IF EXISTS logs_deletes ON reactions;
            DROP TRIGGER IF EXISTS logs_truncates ON reactions;
            DROP TRIGGER IF EXISTS logs_inserts ON reaction_participants;
            DROP TRIGGER IF EXISTS logs_updates ON reaction_participants;
            DROP TRIGGER IF EXISTS logs_deletes ON reaction_participants;
            DROP TRIGGER IF EXISTS logs_truncates ON reaction_participants;
            DROP TRIGGER IF EXISTS logs_inserts ON chemistry_reference;
            DROP TRIGGER IF EXISTS logs_updates ON chemistry_reference;
            DROP TRIGGER IF EXISTS logs_deletes ON chemistry_reference;
            DROP TRIGGER IF EXISTS logs_truncates ON chemistry_reference;
            DROP FUNCTION IF EXISTS log_catalog_rows();
            DROP FUNCTION IF EXISTS log_catalog_truncate();
            DROP TABLE IF EXISTS catalog_changes;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "natural_key",
                table: "substances",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "retired_at",
                table: "substances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "structure_key",
                table: "substances",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "natural_key",
                table: "reactions",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "retired_at",
                table: "reactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "activated_at",
                table: "catalog_releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "answer_version",
                table: "catalog_releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "change_mark",
                table: "catalog_releases",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "retired_at",
                table: "catalog_releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "catalog_releases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(BackfillSql);

            migrationBuilder.CreateIndex(
                name: "ix_substances_natural_key",
                table: "substances",
                column: "natural_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_substances_structure_key",
                table: "substances",
                column: "structure_key");

            migrationBuilder.CreateIndex(
                name: "ix_reactions_natural_key",
                table: "reactions",
                column: "natural_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_releases_status",
                table: "catalog_releases",
                column: "status");

            migrationBuilder.Sql(ChangeLogSql);
            migrationBuilder.Sql(ReleaseTablesSql);
            migrationBuilder.Sql(DatabaseSettingsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropSql);

            migrationBuilder.DropIndex(
                name: "ix_substances_natural_key",
                table: "substances");

            migrationBuilder.DropIndex(
                name: "ix_substances_structure_key",
                table: "substances");

            migrationBuilder.DropIndex(
                name: "ix_reactions_natural_key",
                table: "reactions");

            migrationBuilder.DropIndex(
                name: "ix_catalog_releases_status",
                table: "catalog_releases");

            migrationBuilder.DropColumn(
                name: "natural_key",
                table: "substances");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "substances");

            migrationBuilder.DropColumn(
                name: "structure_key",
                table: "substances");

            migrationBuilder.DropColumn(
                name: "natural_key",
                table: "reactions");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "reactions");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "catalog_releases");

            migrationBuilder.DropColumn(
                name: "answer_version",
                table: "catalog_releases");

            migrationBuilder.DropColumn(
                name: "change_mark",
                table: "catalog_releases");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "catalog_releases");

            migrationBuilder.DropColumn(
                name: "status",
                table: "catalog_releases");
        }
    }
}
