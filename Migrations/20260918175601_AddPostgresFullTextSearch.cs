using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace CvManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgresFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTechnologyTags_TechnologyTags_TechnologyTagId",
                table: "ProjectTechnologyTags");

            migrationBuilder.DropIndex(
                name: "IX_CvAttributeValues_CvId_AttributeDefinitionId",
                table: "CvAttributeValues");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Positions",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Cvs",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_SearchVector",
                table: "Cvs",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_CvAttributeValues_CvId",
                table: "CvAttributeValues",
                column: "CvId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTechnologyTags_TechnologyTags_TechnologyTagId",
                table: "ProjectTechnologyTags",
                column: "TechnologyTagId",
                principalTable: "TechnologyTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTechnologyTags_TechnologyTags_TechnologyTagId",
                table: "ProjectTechnologyTags");

            migrationBuilder.DropIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_SearchVector",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_CvAttributeValues_CvId",
                table: "CvAttributeValues");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Cvs");

            migrationBuilder.CreateIndex(
                name: "IX_CvAttributeValues_CvId_AttributeDefinitionId",
                table: "CvAttributeValues",
                columns: new[] { "CvId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTechnologyTags_TechnologyTags_TechnologyTagId",
                table: "ProjectTechnologyTags",
                column: "TechnologyTagId",
                principalTable: "TechnologyTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
