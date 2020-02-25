using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// Class identifies a unique account.
    /// </summary>
    public class AccountIdentifier
    {
        public string Name { get; set; }
        public int AccountId { get; set; }

        /// <summary>
        /// Method returns a unique path name for this account that contains the account name.
        /// </summary>
        public string GetPathName()
        {
            return $"{Name}_{AccountId}";
        }

        public override string ToString()
        {
            return $"{Name}_{AccountId}";
        }
    }

    /// <summary>
    /// AccountMember class. Defines an account member
    /// </summary>
    public class AccountMember
    {
        #region Constructor

        public AccountMember(string name, AuthorizationLevel level)
        {
            Name = name;
            AuthLevel = level;
        }

        #endregion

        #region Public Properties

        public string Name { get; private set; }
        public AuthorizationLevel AuthLevel { get; private set; }
        public string AuthLevelString {
              get { return AuthLevel.ToString(); }
              private set
              {
                AuthLevel = (AuthorizationLevel)Enum.Parse(typeof(AuthorizationLevel), value); 
              }
         }

        #endregion
    }

    /// <summary>
    /// AccountModel class. Class defines an account including all its details.
    /// </summary>
    public class AccountModel
    {
        #region Public Properties

        public AccountIdentifier Identifier { get; private set; }
        public string Description { get; private set; }
        public string ReportingCurrency { get; private set; }
        public string Type { get; private set; }
        public bool Enabled { get; private set; }
        public string Broker { get; private set; }
        public IList<AccountMember> Members
        {
            get
            {
                return _members;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public AccountModel(AccountIdentifier identifer, string description,
                            string currency, string type, bool enabled, string broker,
                            IList<AccountMember> members)
        {
            Identifier = identifer;
            Description = description ?? Identifier.Name;
            ReportingCurrency = currency;
            Type = type;
            Enabled = enabled;
            Broker = broker;
            AddMembers(members);
        }

        /// <summary>
        /// Method to add an member to the account
        /// </summary>
        public void AddMember(string name, AuthorizationLevel authLevel)
        {
            _members.Add(new AccountMember(name, authLevel));
        }

        /// <summary>
        /// Method to add a list of existing accountmember objects to this account
        /// </summary>
        public void AddMembers(IList<AccountMember> members)
        {
            if (members != null)
            {
                foreach (var member in members)
                {
                    _members.Add(member);
                }
            }
        }

        /// <summary>
        /// remove all members from this account
        /// </summary>
        public void ClearAllMembers()
        {
            _members.Clear();
        }

        /// <summary>
        /// Method updates the account details but not the list of members.
        /// </summary>
        public void UpdateAccountDetails(AccountModel account)
        {
            //uopdate the accunt details for this account from the 
            //account parameter. do not updaye accunt members
            Broker = account.Broker;
            Type = account.Type;
        }

        #endregion


        #region Contract Invariance Methods

        [ContractInvariantMethod]
        protected void ObjectInvariantMethod()
        {
            Contract.Invariant(Identifier != null);
            Contract.Invariant(string.IsNullOrEmpty(Identifier.Name) == false);
            Contract.Invariant(string.IsNullOrEmpty(ReportingCurrency) == false);
            Contract.Invariant(string.IsNullOrEmpty(Type) == false);
            Contract.Invariant(Members != null);
        }

        #endregion

        #region Private Data Members

        private readonly IList<AccountMember> _members =
                    new List<AccountMember>();

        #endregion

    }
}
