using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechChallenge.Models;

namespace TechChallenge.Services
{
    public static class PregaoB3ScrapeService
    {
        private const string BaseUrl = "https://sistemaswebb3-listados.b3.com.br/indexPage/day/IBOV?language=pt-br";
        private const string TableSelector = "table.table.table-responsive-sm.table-responsive-md";
        private static readonly string RowsSelector = TableSelector + " > tbody > tr";
        private const string ColsSelector = "td";
        private const string PagerCounter = "ul.ngx-pagination > li.small-screen";
        private const string NextBtn = "ul.ngx-pagination > li.pagination-next > a";
        private const string DateSelector = "form.ng-untouched.ng-pristine.ng-valid > h2";

        public static async Task<ScrapeResultModel> ScrapeB3DataAsync()
        {
            try
            {
                using var pw = await Playwright.CreateAsync();
                await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
                var page = await (await browser.NewContextAsync()).NewPageAsync();

                await page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.WaitForSelectorAsync(TableSelector);

                var pagerLoc = page.Locator(PagerCounter).First;
                var dateLoc = page.Locator(DateSelector).First;

                string pagerText = (await pagerLoc.InnerTextAsync()).Trim(); // "1 / 5"
                int lastPage = int.Parse(pagerText.Split('/')[1]);

                string dateText = (await dateLoc.InnerTextAsync()).Split('-')[1].Trim();

                var result = new List<IReadOnlyList<string>>();

                while (true)
                {
                    // --------- CAPTURA LINHAS DA PÁGINA ATUAL -----------------
                    var rows = page.Locator(RowsSelector);
                    for (int i = 0, n = await rows.CountAsync(); i < n; i++)
                    {
                        var values = await rows.Nth(i)
                                               .Locator(ColsSelector)
                                               .AllInnerTextsAsync();
                        result.Add(values.Select(v => v.Trim()).ToList());
                    }

                    // --------- VERIFICA SE CHEGOU AO FIM ----------------------
                    int currentPage = int.Parse((await pagerLoc.InnerTextAsync()).Split('/')[0]);
                    if (currentPage >= lastPage) break;

                    // --------- PREPARA SENTINELA DA PRIMEIRA LINHA -----------
                    var firstCellSel = $"{RowsSelector}:nth-child(1) > {ColsSelector}:nth-child(1)";
                    var sentinel = (await page.Locator(firstCellSel).InnerTextAsync()).Trim();

                    // --------- CLICA "PRÓXIMO" E ESPERA A TABELA MUDAR -------
                    await page.ClickAsync(NextBtn);
                    await Expect(page.Locator(firstCellSel)).Not.ToHaveTextAsync(sentinel);
                }

                return new ScrapeResultModel
                {
                    Success = true,
                    Message = "Sucesso",
                    Data = result,
                    TotalRecords = result.Count,
                    Date = dateText
                };
            }
            catch (Exception ex)
            {
                return new ScrapeResultModel
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null,
                    TotalRecords = 0,
                    Date = ""
                };
            }
        }
    }
}
