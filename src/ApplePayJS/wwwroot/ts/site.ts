// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace justEat {

    /**
     * A class representing the handler for Apple Pay JS.
     */
    export class ApplePay {

        public merchantIdentifier: string;
        public storeName: string;

        private applePayVersion: number;
        private countryCode: string;
        private currencyCode: string;
        private session: ApplePaySession;
        private validationResource: string;

        /**
         * Initializes a new instance of the justEat.ApplePay class.
         */
        public constructor() {

            // Get the merchant identifier and store display name from the page meta tags.
            this.merchantIdentifier = $("meta[name='apple-pay-merchant-id']").attr("content");
            this.storeName = $("meta[name='apple-pay-store-name']").attr("content");

            // Get the URL to POST to for Apple Pay merchant validation
            this.validationResource = $("link[rel='merchant-validation']").attr("href");

            // Set the Apple Pay JS version to use
            this.applePayVersion = 10;

            // Set the appropriate ISO country and currency codes
            this.countryCode = $("meta[name='payment-country-code']").attr("content") || "GB";
            this.currencyCode = $("meta[name='payment-currency-code']").attr("content") || "GBP";
        }

        /**
         * Initializes the handler for the current page.
         */
        public initialize(): void {

            if (!this.merchantIdentifier) {
                this.showError("No Apple Pay merchant certificate is configured.");
            }
            // Is ApplePaySession available in the browser?
            else if (this.supportedByDevice() === true) {

                // Determine whether to display the Apple Pay button. See this link for details
                // on the two different approaches: https://developer.apple.com/documentation/applepayjs/checking_if_apple_pay_is_available
                if (this.canMakePayments() === true) {
                    this.showButton();
                }
                else {
                    this.canMakePaymentsWithActiveCard().then((canMakePayments) => {
                        if (canMakePayments === true) {
                            this.showButton();
                        }
                        else {
                            if (this.supportsSetup()) {
                                this.showSetupButton();
                            } else {
                                this.showError("Apple Pay cannot be used at this time. If using macOS you need to be paired with a device that supports at least TouchID.");
                            }
                        }
                    });
                }
            }
            else {
                this.showError("This device and/or browser does not support Apple Pay.");
            }
        }

        /**
         * Handles the Apple Pay button being pressed.
         * @param e - The event object.
         */
        private beginPayment = (e: JQueryEventObject): void => {

            e.preventDefault();

            // Get the amount to request from the form and set up
            // the totals and line items for collection and delivery.
            const subtotal = $("#amount").val().toString();
            const delivery = "0.01";
            const deliveryTotal = (parseFloat(subtotal) + parseFloat(delivery)).toString(10);

            const totalForCollection = {
                label: this.storeName,
                amount: subtotal
            };

            const lineItemsForCollection: ApplePayJS.ApplePayLineItem[] = [
                { label: "Subtotal", amount: subtotal, type: "final" }
            ];

            const totalForDelivery = {
                label: this.storeName,
                amount: deliveryTotal
            };

            const lineItemsForDelivery: ApplePayJS.ApplePayLineItem[] = [
                { label: "Subtotal", amount: subtotal, type: "final" },
                { label: "Delivery", amount: delivery, type: "final" }
            ];

            // Create the Apple Pay payment request as appropriate.
            const paymentRequest = this.createPaymentRequest(delivery, lineItemsForDelivery, totalForDelivery);

            // Create the Apple Pay session.
            this.session = new ApplePaySession(this.applePayVersion, paymentRequest);

            // Setup handler for validation the merchant session.
            this.session.onvalidatemerchant = this.onValidateMerchant;

            // Setup handler for shipping method selection.
            this.session.onshippingmethodselected = (event) => {

                let newTotal;
                let newLineItems;

                // Swap the total and line items based on the selected shipping method
                if (event.shippingMethod.identifier === "collection") {
                    newTotal = totalForCollection;
                    newLineItems = lineItemsForCollection;
                }
                else {
                    newTotal = totalForDelivery;
                    newLineItems = lineItemsForDelivery;
                }

                const update = {
                    newTotal: newTotal,
                    newLineItems: newLineItems
                };

                this.session.completeShippingMethodSelection(update);
            };

            // Setup handler to receive the token when payment is authorized.
            this.session.onpaymentauthorized = this.onPaymentAuthorized;

            // Begin the session to display the Apple Pay sheet.
            this.session.begin();
        }

        /**
         * Captures funds from the specified payment token.
         * @param token - The authorized Apple Pay payment token.
         * @returns The authorization result to return to complete the payment.
         */
        private captureFunds(token: ApplePayJS.ApplePayPaymentToken): ApplePayJS.ApplePayPaymentAuthorizationResult {

            // Do something with the payment to capture funds and
            // then dismiss the Apple Pay sheet for the session with
            // the relevant status code for the payment's authorization.
            // If any errors occurred, add them to the errors array for display.
            return {
                status: ApplePaySession.STATUS_SUCCESS,
                errors: []
            };
        }

        /**
         * Indicates whether or not the device supports Apple Pay.
         * @returns true if the device supports making payments with Apple Pay; otherwise, false.
         */
        private canMakePayments(): boolean {
            return ApplePaySession.canMakePayments();
        }

        /**
         * Indicates whether or not the device supports Apple Pay and if the user has an active card in Wallet.
         * @returns true if the device supports Apple Pay and there is at least one active card in Wallet; otherwise, false.
         */
        private canMakePaymentsWithActiveCard(): Promise<boolean> {
            return ApplePaySession.canMakePaymentsWithActiveCard(this.merchantIdentifier);
        }

        /**
         * Creates an Apple Pay payment request for the specified total and line items.
         * @param deliveryAmount - The amount to charge for delivery.
         * @param lineItems - The line items for the payment.
         * @param total - The total for the payment.
         * @returns The Apple Pay payment request that was created.
         */
        private createPaymentRequest = (deliveryAmount: string, lineItems: ApplePayJS.ApplePayLineItem[], total: ApplePayJS.ApplePayLineItem): ApplePayJS.ApplePayPaymentRequest => {
            let paymentRequest: ApplePayJS.ApplePayPaymentRequest = {
                applicationData: btoa("Custom application-specific data"),
                countryCode: this.countryCode,
                currencyCode: this.currencyCode,
                merchantCapabilities: ["supports3DS", "supportsCredit", "supportsDebit"],
                supportedNetworks: ["amex", "discover", "jcb", "masterCard", "privateLabel", "visa"],
                lineItems: lineItems,
                total: total,
                requiredBillingContactFields: ["email", "name", "phone", "postalAddress"],
                requiredShippingContactFields: ["email", "name", "phone", "postalAddress"],
                shippingType: "delivery",
                shippingMethods: [
                    { label: "Delivery", amount: deliveryAmount, identifier: "delivery", detail: "Delivery to you" },
                    { label: "Collection", amount: "0.00", identifier: "collection", detail: "Collect from the store" }
                ],
                supportedCountries: [this.countryCode]
            };

            // You can optionally pre-populate the billing and shipping contact
            // with information about the current user, if available to you.
            // paymentRequest.billingContact = {
            //     givenName: "",
            //     familyName: ""
            // };
            // paymentRequest.shippingContact = {
            //     givenName: "",
            //     familyName: ""
            // };

            return paymentRequest;
        }

        /**
         * Gets the current page's language.
         * @returns The current page language.
         */
        private getPageLanguage(): string {
            return $("html").attr("lang") || "en";
        }

        /**
         * Hides the setup button.
         */
        private hideSetupButton(): void {
            const button = $("#set-up-apple-pay-button");
            button.addClass("d-none");
            button.off("click");
        }

        /**
         * Handles the Apple Pay payment being authorized by the user.
         * @param event - The event object.
         */
        private onPaymentAuthorized = (event: ApplePayJS.ApplePayPaymentAuthorizedEvent): void => {

            // Get the payment data for use to capture funds from
            // the encrypted Apple Pay token in your server.
            const token = event.payment.token;

            // Process the payment
            const authorizationResult = this.captureFunds(token);

            if (authorizationResult.status === ApplePaySession.STATUS_SUCCESS) {

                // Get the contact details for use, for example to
                // use to create an account for the user.
                const billingContact = event.payment.billingContact;
                const shippingContact = event.payment.shippingContact;

                // Apply the details captured from the Apple Pay sheet to the page.
                $(".card-name").text(event.payment.token.paymentMethod.displayName);
                this.updatePanel($("#billing-contact"), billingContact);
                this.updatePanel($("#shipping-contact"), shippingContact);
                this.showSuccess();
            }
            else {
                const errors = authorizationResult.errors.map((error) => {
                    return error.message;
                });
                this.showError(`Your payment could not be processed. ${errors.join(" ")}`);
                authorizationResult.errors.forEach((error) => {
                    console.error(`${error.message} (${error.contactField}: ${error.code}).`);
                });
            }

            this.session.completePayment(authorizationResult);
        }

        /**
         * Handles merchant validation for the Apple Pay session.
         * @param event - The event object.
         */
        private onValidateMerchant = (event: ApplePayJS.ApplePayValidateMerchantEvent): void => {

            // Create the payload.
            const data = {
                validationUrl: event.validationURL
            };

            const headers = this.createValidationHeaders();
            const request = this.createValidationRequest(data, headers);

            // Post the payload to the server to validate the
            // merchant session using the merchant certificate.
            $.ajax(request).then((merchantSession) => {
                // Complete validation by passing the merchant session to the Apple Pay session.
                this.session.completeMerchantValidation(merchantSession);
            });
        }

        /**
         * Creates the HTTP headers to use for the validation request.
         * @returns An object representing the HTTP headers for the request.
         */
        private createValidationHeaders(): any {

            // Set any custom HTTP request headers here.
            let headers: any = {
            };

            // Setup antiforgery HTTP header.
            const antiforgeryHeader = $("meta[name='x-antiforgery-name']").attr("content");
            const antiforgeryToken = $("meta[name='x-antiforgery-token']").attr("content");

            headers[antiforgeryHeader] = antiforgeryToken;

            return headers;
        }

        /**
         * Creates the validation request to use for the HTTP POST to the server.
         * @param data - The request data.
         * @param headers - The request headers.
         */
        private createValidationRequest = (data: any, headers: any): JQueryAjaxSettings => {
            return {
                url: this.validationResource,
                method: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(data),
                headers: headers
            };
        }

        /**
         * Event handler for setting up Apple Pay.
         */
        private setupApplePay = (): Promise<boolean> => {
            return ApplePaySession.openPaymentSetup(this.merchantIdentifier)
                .then((success) => {
                    if (success) {
                        this.hideSetupButton();
                        this.showButton();
                    }
                    else {
                        this.showError("Failed to set up Apple Pay.");
                    }
                    return success;
                }).catch((err: any) => {
                    this.showError(`Failed to set up Apple Pay. ${JSON.stringify(err)}`);
                    return false;
                });
        }

        /**
         * Shows the Apple Pay button.
         */
        private showButton = (): void => {

            const button = $("#apple-pay-button");
            button.attr("lang", this.getPageLanguage());
            button.on("click", this.beginPayment);

            if (this.supportsSetup()) {
                button.addClass("apple-pay-button-with-text");
                button.addClass("apple-pay-button-black-with-text");
            }
            else {
                button.addClass("apple-pay-button");
                button.addClass("apple-pay-button-black");
            }

            button.removeClass("d-none");
        }

        /**
         * Shows the error banner.
         * @param text - The text to show in the banner.
         */
        private showError(text: string): void {
            const error = $(".apple-pay-error");
            error.text(text);
            error.removeClass("d-none");
        }

        /**
         * Shows the button to set up Apple Pay.
         */
        private showSetupButton = (): void => {
            const button = $("#set-up-apple-pay-button");
            button.attr("lang", this.getPageLanguage());
            button.on("click", this.setupApplePay);
            button.removeClass("d-none");
        }

        /**
         * Shows the successful payment button.
         */
        private showSuccess() {
            $(".apple-pay-intro").hide();
            const success = $(".apple-pay-success");
            success.removeClass("d-none");
        }

        /**
         * Returns whether Apple Pay is supported on the current device.
         * @returns Whether Apple Pay is supported.
         */
        private supportedByDevice(): boolean {
            return "ApplePaySession" in window && ApplePaySession !== undefined;
        }

        /**
         * Returns whether setting up Apple Pay is supported on the current device.
         * @returns Whether setting up Apple Pay is supported.
         */
        private supportsSetup(): boolean {
            return "openPaymentSetup" in ApplePaySession;
        }

        /**
         * Updates the specified panel with the specified Apple Pay contact.
         * @param panel - The panel to update.
         * @param contact - The contact to update the panel with the details for.
         */
        private updatePanel = (panel: JQuery, contact: ApplePayJS.ApplePayPaymentContact) => {

            if (contact.emailAddress) {
                panel.find(".contact-email")
                    .text(contact.emailAddress)
                    .attr("href", "mailto:" + contact.emailAddress)
                    .append("<br/>")
                    .removeClass("d-none");
            }

            if (contact.phoneNumber) {
                panel.find(".contact-telephone")
                    .text(contact.phoneNumber)
                    .attr("href", "tel:" + contact.phoneNumber)
                    .append("<br/>")
                    .removeClass("d-none");
            }

            if (contact.givenName) {
                panel.find(".contact-name")
                    .text(contact.givenName + " " + contact.familyName)
                    .append("<br/>")
                    .removeClass("d-none");
            }

            if (contact.addressLines) {
                panel.find(".contact-address-lines").text(contact.addressLines.join(", "));
                panel.find(".contact-sub-locality").text(contact.subLocality);
                panel.find(".contact-locality").text(contact.locality);
                panel.find(".contact-sub-administrative-area").text(contact.subAdministrativeArea);
                panel.find(".contact-administrative-area").text(contact.administrativeArea);
                panel.find(".contact-postal-code").text(contact.postalCode);
                panel.find(".contact-country").text(contact.country);
                panel.find(".contact-address").removeClass("d-none");
            }
        }
    }
}

(() => {
    const handler = new justEat.ApplePay();
    handler.initialize();
})();
