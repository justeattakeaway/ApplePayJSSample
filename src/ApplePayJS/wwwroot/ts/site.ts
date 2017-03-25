// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace justEat {

    /**
     * A class representing the handler for Apple Pay JS.
     */
    export class ApplePay {

        public merchantIdentifier: string;

        private applePayVersion: number;

        /**
         * Initializes a new instance of the justEat.ApplePay class.
         */
        public constructor() {

            // Get the merchant identifier from the page meta tags.
            this.merchantIdentifier = $("meta[name='apple-pay-merchant-id']").attr("content");

            // Set the Apple Pay JS version to use
            this.applePayVersion = 1;
        }

        /**
         * Initializes the handler for the current page.
         */
        public initialize(): void {

            // Is ApplePaySession available in the browser?
            if (this.supportedByDevice() === true) {

                // Determine whether to display the Apple Pay button. See this link for details
                // on the two different approaches: https://developer.apple.com/reference/applepayjs/applepaysession#2168855
                if (this.canMakePayments() === true) {
                    this.showButton();
                } else {
                    this.canMakePaymentsWithActiveCard().then((canMakePayments: boolean) => {
                        if (canMakePayments === true) {
                            this.showButton();
                        } else {
                            if (this.supportsSetup()) {
                                this.showSetupButton();
                            } else {
                                this.showError("Apple Pay cannot be used at this time. If using macOS Sierra you need to be paired with a device that supports TouchID.");
                            }
                        }
                    });
                }
            } else {
                this.showError("This device and/or browser does not support Apple Pay.");
            }
        }

        private beginPayment = (e: JQueryEventObject): void => {

            e.preventDefault();

            // Get the amount to request from the form and set up
            // the totals and line items for collection and delivery.
            let subtotal = $("#amount").val();
            let delivery = "0.01";
            let deliveryTotal = (parseFloat(subtotal) + parseFloat(delivery)).toString(10);
            let storeName = $("meta[name='apple-pay-store-name']").attr("content");

            let totalForCollection = {
                label: storeName,
                amount: subtotal
            };

            let lineItemsForCollection = [
                { label: "Subtotal", amount: subtotal, type: "final" }
            ];

            let totalForDelivery = {
                label: storeName,
                amount: deliveryTotal
            };

            let lineItemsForDelivery = [
                { label: "Subtotal", amount: subtotal, type: "final" },
                { label: "Delivery", amount: delivery, type: "final" }
            ];

            // Create the Apple Pay payment request as appropriate.
            let paymentRequest = {
                countryCode: "GB",
                currencyCode: "GBP",
                merchantCapabilities: ["supports3DS"],
                supportedNetworks: ["amex", "masterCard", "visa"],
                lineItems: lineItemsForDelivery,
                total: totalForDelivery,
                requiredBillingContactFields: ["email", "name", "phone", "postalAddress"],
                requiredShippingContactFields: ["email", "name", "phone", "postalAddress"],
                shippingType: "delivery",
                shippingMethods: [
                    { label: "Delivery", amount: delivery, identifier: "delivery", detail: "Delivery to you" },
                    { label: "Collection", amount: "0.00", identifier: "collection", detail: "Collect from the store" }
                ]
            };

            // Create the Apple Pay session.
            let session = new ApplePayJS.ApplePaySession(1, paymentRequest);

            // Setup handler for validation the merchant session.
            session.onvalidatemerchant = function (event) {

                // Create the payload.
                let data = {
                    validationUrl: event.validationURL
                };

                // Setup antiforgery HTTP header.
                let antiforgeryHeader = $("meta[name='x-antiforgery-name']").attr("content");
                let antiforgeryToken = $("meta[name='x-antiforgery-token']").attr("content");

                let headers: any = {};
                headers[antiforgeryHeader] = antiforgeryToken;

                // Post the payload to the server to validate the
                // merchant session using the merchant certificate.
                $.ajax({
                    url: "/home/validate",
                    method: "POST",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify(data),
                    headers: headers
                }).then((merchantSession) => {
                    // Complete validation by passing the merchant session to the Apple Pay session.
                    session.completeMerchantValidation(merchantSession);
                });
            };

            // Setup handler for shipping method selection.
            session.onshippingmethodselected = (event) => {

                let newTotal;
                let newLineItems;

                if (event.shippingMethod.identifier === "collection") {
                    newTotal = totalForCollection;
                    newLineItems = lineItemsForCollection;
                } else {
                    newTotal = totalForDelivery;
                    newLineItems = lineItemsForDelivery;
                }

                session.completeShippingMethodSelection(ApplePayJS.ApplePaySession.STATUS_SUCCESS, newTotal, newLineItems);
            };

            // Setup handler to receive the token when payment is authorized.
            session.onpaymentauthorized = (event) => {

                // Get the contact details for use, for example to
                // use to create an account for the user.
                let billingContact = event.payment.billingContact;
                let shippingContact = event.payment.shippingContact;

                // Get the payment data for use to capture funds from
                // the encrypted Apple Pay token in your server.
                let token = event.payment.token.paymentData;

                // Apply the details from the Apple Pay sheet to the page.
                let update = (panel: JQuery, contact: ApplePayJS.ApplePayPaymentContact) => {

                    if (contact.emailAddress) {
                        panel.find(".contact-email")
                            .text(contact.emailAddress)
                            .attr("href", "mailto:" + contact.emailAddress)
                            .append("<br/>")
                            .removeClass("hide");
                    }

                    if (contact.emailAddress) {
                        panel.find(".contact-telephone")
                            .text(contact.phoneNumber)
                            .attr("href", "tel:" + contact.phoneNumber)
                            .append("<br/>")
                            .removeClass("hide");
                    }

                    if (contact.givenName) {
                        panel.find(".contact-name")
                            .text(contact.givenName + " " + contact.familyName)
                            .append("<br/>")
                            .removeClass("hide");
                    }

                    if (contact.addressLines) {
                        panel.find(".contact-address-lines").text(contact.addressLines.join(", "));
                        panel.find(".contact-locality").text(contact.locality);
                        panel.find(".contact-administrative-area").text(contact.administrativeArea);
                        panel.find(".contact-postal-code").text(contact.postalCode);
                        panel.find(".contact-country").text(contact.country);
                        panel.find(".contact-address").removeClass("hide");
                    }
                };

                $(".card-name").text(event.payment.token.paymentMethod.displayName);
                update($("#billing-contact"), billingContact);
                update($("#shipping-contact"), shippingContact);

                // Do something with the payment to capture funds and
                // then dismiss the Apple Pay sheet for the session with
                // the relevant status code for the payment's authorization.
                session.completePayment(ApplePayJS.ApplePaySession.STATUS_SUCCESS);

                this.showSuccess();
            };

            // Start the session to display the Apple Pay sheet.
            session.begin();
        }

        /**
         * Indicates whether or not the device supports Apple Pay.
         * @returns true if the device supports making payments with Apple Pay; otherwise, false.
         */
        private canMakePayments(): boolean {
            return ApplePayJS.ApplePaySession.canMakePayments();
        }

        /**
         * Indicates whether or not the device supports Apple Pay and if the user has an active card in Wallet.
         * @returns true if the device supports Apple Pay and there is at least one active card in Wallet; otherwise, false.
         */
        private canMakePaymentsWithActiveCard(): Promise<boolean> {
            return ApplePayJS.ApplePaySession.canMakePaymentsWithActiveCard(this.merchantIdentifier);
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
            let button = $("#set-up-apple-pay-button");
            button.addClass("hide");
            button.off("click");
        }

        private setupApplePay = (): Promise<boolean> => {
            return ApplePayJS.ApplePaySession.openPaymentSetup(this.merchantIdentifier)
                .then((success) => {
                    if (success) {
                        this.hideSetupButton();
                        this.showButton();
                    } else {
                        this.showError("Failed to set up Apple Pay.");
                    }
                    return success;
                }).catch((e: any) => {
                    this.showError("Failed to set up Apple Pay. " + e);
                    return false;
                });
        }

        private showButton = (): void => {

            let button = $("#apple-pay-button");
            button.attr("lang", this.getPageLanguage());
            button.on("click", this.beginPayment);

            if (this.supportsSetup()) {
                button.addClass("apple-pay-button-with-text");
                button.addClass("apple-pay-button-black-with-text");
            } else {
                button.addClass("apple-pay-button");
                button.addClass("apple-pay-button-black");
            }

            button.removeClass("hide");
        }

        /**
         * Shows the error banner.
         * @param text - The text to show in the banner.
         */
        private showError(text: string): void {
            let error = $(".apple-pay-error");
            error.text(text);
            error.removeClass("hide");
        }

        private showSetupButton = (): void => {
            let button = $("#set-up-apple-pay-button");
            button.attr("lang", this.getPageLanguage());
            button.on("click", this.setupApplePay);
            button.removeClass("hide");
        }

        /**
         * Shows the successful payment button.
         */
        private showSuccess() {
            $(".apple-pay-intro").hide();
            let success = $(".apple-pay-success");
            success.removeClass("hide");
        }

        /**
         * Returns whether Apple Pay is supported on the current device.
         * @returns Whether Apple Pay is supported.
         */
        private supportedByDevice(): boolean {
            return "ApplePaySession" in Window;
        }

        /**
         * Returns whether setting up Apple Pay is supported on the current device.
         * @returns Whether setting up Apple Pay is supported.
         */
        private supportsSetup(): boolean {
            return "openPaymentSetup" in ApplePayJS.ApplePaySession;
        }
    }
}

(() => {
    const handler = new justEat.ApplePay();
    handler.initialize();
})();
