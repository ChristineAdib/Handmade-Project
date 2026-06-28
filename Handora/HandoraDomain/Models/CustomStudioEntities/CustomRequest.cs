using System;
using System.Collections.Generic;
using System.Linq;
using HandoraDomain.Consts;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.PaymentEntities;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class CustomRequest : BaseEntity<Guid>
    {
        public ProductType ProductType { get; set; } = ProductType.CrochetDoll;
        public CustomRequestStatus Status { get; set; } = CustomRequestStatus.Draft;
        public WizardStep WizardStep { get; set; } = WizardStep.Initial;
        public int GenerationCount { get; set; } = 0;

        public decimal? TargetBudget { get; set; }
        public DateTime? DeadlineDate { get; set; }

        // Selected References
        public Guid? SelectedDesignId { get; set; }
        public GeneratedDesign? SelectedDesign { get; set; }

        public Guid? SelectedSellerId { get; set; }
        public Shop? SelectedSeller { get; set; }

        // Buyer reference
        public string BuyerId { get; set; } = string.Empty;
        public User Buyer { get; set; } = null!;

        // Navigation Properties
        public CustomConfiguration? CustomConfiguration { get; set; }
        public ICollection<GeneratedDesign> GeneratedDesigns { get; set; } = new List<GeneratedDesign>();
        public ICollection<SellerRecommendation> SellerRecommendations { get; set; } = new List<SellerRecommendation>();
        public ICollection<CustomOffer> CustomOffers { get; set; } = new List<CustomOffer>();
        public CustomService? CustomService { get; set; }
        public ProjectWorkspace? ProjectWorkspace { get; set; }

        #region Domain Logic & State Machine Transitions

        public void Configure(CustomConfiguration configuration)
        {
            if (Status != CustomRequestStatus.Draft && Status != CustomRequestStatus.Configuring)
            {
                throw new InvalidOperationException("Can only configure requests in Draft or Configuring state.");
            }

            CustomConfiguration = configuration;
            Status = CustomRequestStatus.Configuring;
            WizardStep = WizardStep.Styling;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SubmitForGeneration()
        {
            if (Status != CustomRequestStatus.Configuring)
            {
                throw new InvalidOperationException("Configuration must be in progress to submit for generation.");
            }

            if (CustomConfiguration == null || string.IsNullOrWhiteSpace(CustomConfiguration.ConfigurationDataJson))
            {
                throw new InvalidOperationException("Valid custom configuration details are required before generation.");
            }

            Status = CustomRequestStatus.ReadyForGeneration;
            WizardStep = WizardStep.Review;
            UpdatedAt = DateTime.UtcNow;
        }

        public void StartGeneration(int maxGenerations)
        {
            if (Status != CustomRequestStatus.ReadyForGeneration &&
                Status != CustomRequestStatus.Generated &&
                Status != CustomRequestStatus.DesignSelected &&
                Status != CustomRequestStatus.SellerMatched &&
                Status != CustomRequestStatus.Negotiation)
            {
                throw new InvalidOperationException("Request must be ready for generation or already have generated designs to run another generation.");
            }

            if (GenerationCount >= maxGenerations)
            {
                throw new InvalidOperationException($"Generation count limit reached. Maximum allowed AI generations is {maxGenerations}.");
            }

            Status = CustomRequestStatus.Generating;
            GenerationCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CompleteGeneration(GeneratedDesign design)
        {
            if (Status != CustomRequestStatus.Generating && Status != CustomRequestStatus.Generated)
            {
                throw new InvalidOperationException("Can only complete a generation currently in progress.");
            }

            GeneratedDesigns.Add(design);
            Status = CustomRequestStatus.Generated;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SelectDesign(Guid designId)
        {
            if (Status != CustomRequestStatus.Generated &&
                Status != CustomRequestStatus.DesignSelected &&
                Status != CustomRequestStatus.SellerMatched &&
                Status != CustomRequestStatus.Negotiation)
            {
                throw new InvalidOperationException("Design selection can only occur on generated or negotiating requests.");
            }

            var exists = GeneratedDesigns.Any(d => d.Id == designId);
            if (!exists)
            {
                throw new InvalidOperationException("The selected design does not belong to this custom request.");
            }

            SelectedDesignId = designId;
            // Only advance to DesignSelected if we're still at Generated;
            // preserve higher states (SellerMatched, Negotiation) so we don't
            // break the downstream workflow.
            if (Status == CustomRequestStatus.Generated)
            {
                Status = CustomRequestStatus.DesignSelected;
            }
            UpdatedAt = DateTime.UtcNow;
        }

        public void MatchSellers(List<SellerRecommendation> recommendations)
        {
            if (Status != CustomRequestStatus.DesignSelected)
            {
                throw new InvalidOperationException("A design must be selected before seller matching recommendations can run.");
            }

            if (recommendations == null || recommendations.Count == 0)
            {
                throw new ArgumentException("Seller recommendations list cannot be empty.", nameof(recommendations));
            }

            foreach (var rec in recommendations)
            {
                SellerRecommendations.Add(rec);
            }

            Status = CustomRequestStatus.SellerMatched;
            UpdatedAt = DateTime.UtcNow;
        }

        public void OpenForNegotiation()
        {
            if (Status != CustomRequestStatus.SellerMatched)
            {
                throw new InvalidOperationException("Sellers must be matched before opening for negotiation.");
            }

            Status = CustomRequestStatus.Negotiation;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ReceiveOffer(CustomOffer offer)
        {
            if (Status == CustomRequestStatus.Draft || Status == CustomRequestStatus.Configuring || Status == CustomRequestStatus.ReadyForGeneration || Status == CustomRequestStatus.Generating)
            {
                throw new InvalidOperationException("Custom request is not open for receiving offers.");
            }

            // Ensure offer matches request
            offer.CustomRequestId = Id;
            if (!CustomOffers.Any(o => o.Id == offer.Id))
            {
                CustomOffers.Add(offer);
            }

            if (Status == CustomRequestStatus.DesignSelected || Status == CustomRequestStatus.SellerMatched || Status == CustomRequestStatus.Negotiation)
            {
                Status = CustomRequestStatus.OfferSent;
            }
            
            UpdatedAt = DateTime.UtcNow;
        }

        public void AcceptOffer(Guid offerId, Conversation conversation)
        {
            if (Status != CustomRequestStatus.OfferSent && Status != CustomRequestStatus.Negotiation)
            {
                throw new InvalidOperationException("Can only accept an offer when in negotiation or offer sent states.");
            }

            var offer = CustomOffers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null)
            {
                throw new InvalidOperationException("Specified offer does not belong to this custom request.");
            }

            offer.Status = OfferStatus.Accepted;
            offer.AcceptedAt = DateTime.UtcNow;

            SelectedSellerId = offer.ShopId;
            Status = CustomRequestStatus.OfferAccepted;

            UpdatedAt = DateTime.UtcNow;
        }

        public void InitiatePayment()
        {
            if (Status != CustomRequestStatus.OfferAccepted)
            {
                throw new InvalidOperationException("Payment can only be initiated after accepting a seller's offer.");
            }

            Status = CustomRequestStatus.PaymentPending;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CompletePayment()
        {
            if (Status != CustomRequestStatus.PaymentPending)
            {
                throw new InvalidOperationException("Can only complete payment on custom requests in PaymentPending status.");
            }

            Status = CustomRequestStatus.Paid;
            UpdatedAt = DateTime.UtcNow;
        }

        public void StartWork()
        {
            if (Status != CustomRequestStatus.Paid)
            {
                throw new InvalidOperationException("Work can only begin after the custom request is paid.");
            }

            if (ProjectWorkspace == null)
            {
                throw new InvalidOperationException("Project workspace not found.");
            }

            Status = CustomRequestStatus.InProgress;
            ProjectWorkspace.Status = ProjectWorkspaceStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CompleteProject()
        {
            if (Status != CustomRequestStatus.InProgress)
            {
                throw new InvalidOperationException("Only projects currently InProgress can be completed.");
            }

            if (ProjectWorkspace == null)
            {
                throw new InvalidOperationException("Project workspace not found.");
            }

            Status = CustomRequestStatus.Completed;
            ProjectWorkspace.Status = ProjectWorkspaceStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == CustomRequestStatus.Completed || Status == CustomRequestStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot cancel custom requests that are already completed or cancelled.");
            }

            // Reject all pending offers if cancelled early
            foreach (var offer in CustomOffers.Where(o => o.Status == OfferStatus.Pending))
            {
                offer.Status = OfferStatus.Withdrawn;
            }

            Status = CustomRequestStatus.Cancelled;
            if (ProjectWorkspace != null)
            {
                ProjectWorkspace.Status = ProjectWorkspaceStatus.Refunded;
            }

            UpdatedAt = DateTime.UtcNow;
        }

        #endregion
    }
}
