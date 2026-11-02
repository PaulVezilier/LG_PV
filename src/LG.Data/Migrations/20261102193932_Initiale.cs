using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LG.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initiale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ambiances",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ambiances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    camp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skins",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    fichier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ambiance_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    gratuit = table.Column<bool>(type: "boolean", nullable: false),
                    actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skins", x => x.id);
                    table.ForeignKey(
                        name: "fk_skins_ambiances_ambiance_id",
                        column: x => x.ambiance_id,
                        principalTable: "ambiances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comptes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inscrit_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    skin_id = table.Column<int>(type: "integer", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comptes", x => x.id);
                    table.ForeignKey(
                        name: "fk_comptes_skins_skin_id",
                        column: x => x.skin_id,
                        principalTable: "skins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "comptes_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comptes_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_comptes_claims_comptes_user_id",
                        column: x => x.user_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comptes_connexions_externes",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comptes_connexions_externes", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_comptes_connexions_externes_comptes_user_id",
                        column: x => x.user_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comptes_jetons",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comptes_jetons", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_comptes_jetons_comptes_user_id",
                        column: x => x.user_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    hote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ambiance_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creee_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    commencee_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    terminee_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nombre_tours = table.Column<int>(type: "integer", nullable: false),
                    camp_vainqueur = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parties", x => x.id);
                    table.ForeignKey(
                        name: "fk_parties_ambiances_ambiance_id",
                        column: x => x.ambiance_id,
                        principalTable: "ambiances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parties_users_hote_id",
                        column: x => x.hote_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "skins_possedes",
                columns: table => new
                {
                    compte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skin_id = table.Column<int>(type: "integer", nullable: false),
                    obtenu_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skins_possedes", x => new { x.compte_id, x.skin_id });
                    table.ForeignKey(
                        name: "fk_skins_possedes_skins_skin_id",
                        column: x => x.skin_id,
                        principalTable: "skins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_skins_possedes_users_compte_id",
                        column: x => x.compte_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evenements_partie",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    partie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tour = table.Column<int>(type: "integer", nullable: false),
                    phase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    texte = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    auteur_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destinataires = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evenements_partie", x => x.id);
                    table.ForeignKey(
                        name: "fk_evenements_partie_comptes_auteur_id",
                        column: x => x.auteur_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_evenements_partie_parties_partie_id",
                        column: x => x.partie_id,
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    partie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    compte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pseudo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    siege = table.Column<int>(type: "integer", nullable: false),
                    skin_id = table.Column<int>(type: "integer", nullable: true),
                    role_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    role_revele = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    camp_final = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    survivant = table.Column<bool>(type: "boolean", nullable: true),
                    vainqueur = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => new { x.partie_id, x.compte_id });
                    table.ForeignKey(
                        name: "fk_participants_parties_partie_id",
                        column: x => x.partie_id,
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_participants_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_participants_skins_skin_id",
                        column: x => x.skin_id,
                        principalTable: "skins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_participants_users_compte_id",
                        column: x => x.compte_id,
                        principalTable: "comptes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "comptes",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_comptes_skin_id",
                table: "comptes",
                column: "skin_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "comptes",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comptes_claims_user_id",
                table: "comptes_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_comptes_connexions_externes_user_id",
                table: "comptes_connexions_externes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_evenements_partie_auteur_id",
                table: "evenements_partie",
                column: "auteur_id");

            migrationBuilder.CreateIndex(
                name: "ix_evenements_partie_partie_id_numero",
                table: "evenements_partie",
                columns: new[] { "partie_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participants_compte_id",
                table: "participants",
                column: "compte_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_role_id",
                table: "participants",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_skin_id",
                table: "participants",
                column: "skin_id");

            migrationBuilder.CreateIndex(
                name: "ix_parties_ambiance_id",
                table: "parties",
                column: "ambiance_id");

            migrationBuilder.CreateIndex(
                name: "ix_parties_code",
                table: "parties",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_parties_hote_id",
                table: "parties",
                column: "hote_id");

            migrationBuilder.CreateIndex(
                name: "ix_parties_statut",
                table: "parties",
                column: "statut");

            migrationBuilder.CreateIndex(
                name: "ix_skins_ambiance_id",
                table: "skins",
                column: "ambiance_id");

            migrationBuilder.CreateIndex(
                name: "ix_skins_code",
                table: "skins",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skins_possedes_skin_id",
                table: "skins_possedes",
                column: "skin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comptes_claims");

            migrationBuilder.DropTable(
                name: "comptes_connexions_externes");

            migrationBuilder.DropTable(
                name: "comptes_jetons");

            migrationBuilder.DropTable(
                name: "evenements_partie");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "skins_possedes");

            migrationBuilder.DropTable(
                name: "parties");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "comptes");

            migrationBuilder.DropTable(
                name: "skins");

            migrationBuilder.DropTable(
                name: "ambiances");
        }
    }
}
