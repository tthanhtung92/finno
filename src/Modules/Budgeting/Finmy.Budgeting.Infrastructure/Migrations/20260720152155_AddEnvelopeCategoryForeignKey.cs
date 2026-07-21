using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finmy.Budgeting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeCategoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Envelopes_Categories_CategoryId",
                schema: "budgeting",
                table: "Envelopes",
                column: "CategoryId",
                principalSchema: "budgeting",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Envelopes_Categories_CategoryId",
                schema: "budgeting",
                table: "Envelopes");
        }
    }
}
