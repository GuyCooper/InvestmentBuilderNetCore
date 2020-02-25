using InvestmentBuilderCore;
using System;
using System.Collections.Generic;
using System.Text;
using Transports;

namespace InvestmentBuilderManager.Dtos
{
    /// <summary>
    /// AccountMember dto.
    /// </summary>
    internal class AccountMemberDto
    {
        public string Name { get; set; }
        public string Permission { get; set; }
    }

    /// <summary>
    /// AccountDetails Dto
    /// </summary>
    internal class AccountDetailsDto : Dto
    {
        public AccountIdentifier AccountName { get; set; }
        public string Description { get; set; }
        public string ReportingCurrency { get; set; }
        public string AccountType { get; set; }
        public bool Enabled { get; set; }
        public string Broker { get; set; }
        public IList<AccountMemberDto> Members { get; set; }
    }

    /// <summary>
    /// Response to update account details. status flag indicates failure / success.
    /// if success, returns new list of accounts for user
    /// </summary>
    internal class UpdateAccountResponseDto : Dto
    {
        public bool Status { get; set; }
        public IEnumerable<AccountIdentifier> AccountNames { get; set; }
    }

}
