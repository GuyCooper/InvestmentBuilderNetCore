using InvestmentBuilderCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transports;
using Transports.Session;

namespace TestClient
{
    class PortfolioResponseDto : Dto
    {
        public IEnumerable<CompanyData> Portfolio { get; private set; }

        public PortfolioResponseDto(IEnumerable<CompanyData> portfolio)
        {
            Portfolio = portfolio;
        }
    }

    internal sealed class GetPortfolioOperation : Operation<Dto, PortfolioResponseDto>
    {
        public GetPortfolioOperation(IConnectionSession session) : base(session, "GetPortfolio request, ", "GET_PORTFOLIO_REQUEST", "GET_PORTFOLIO_RESPONSE")
        { }

        protected override Dto GetRequest()
        {
            return new Dto();
        }

        protected override bool HandleResponse(PortfolioResponseDto response)
        {
            var companies = response.Portfolio.ToList();
            return companies.Count == 31;

        }
    }
}
