// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

justEat = {
    applePay: {
        // Function to handle payment when the Apple Pay button is clicked/pressed.
        beginPayment: function (e) {

            e.preventDefault();

            // Get the amount to request from the form and set up
            // the totals and line items for collection and delivery.
            var subtotal = $("#amount").val();
            var delivery = "0.01";
            var deliveryTotal = (Number(subtotal) + Number(delivery)).toString();

            var countryCode = $("meta[name='payment-country-code']").attr("content") || "GB";
            var currencyCode = $("meta[name='payment-currency-code']").attr("content") || "GBP";
            var storeName = $("meta[name='apple-pay-store-name']").attr("content");

            var totalForCollection = {
                label: storeName,
                amount: subtotal
            };

            var lineItemsForCollection = [
                { label: "Subtotal", amount: subtotal, type: "final" }
            ];

            var totalForDelivery = {
                label: storeName,
                amount: deliveryTotal
            };

            var lineItemsForDelivery = [
                { label: "Subtotal", amount: subtotal, type: "final" },
                { label: "Delivery", amount: delivery, type: "final" }
            ];

            // Create the Apple Pay payment request as appropriate.
            var paymentRequest = {
                applicationData: btoa("Custom application-specific data"),
                countryCode: countryCode,
                currencyCode: currencyCode,
                merchantCapabilities: [ "supports3DS" ],
                supportedNetworks: [ "amex", "masterCard", "visa" ],
                lineItems: lineItemsForDelivery,
                total: totalForDelivery,
                requiredBillingContactFields: [ "email", "name", "phone", "postalAddress" ],
                requiredShippingContactFields: [ "email", "name", "phone", "postalAddress" ],
                shippingType: "delivery",
                shippingMethods: [
                    { label: "Delivery", amount: delivery, identifier: "delivery", detail: "Delivery to you" },
                    { label: "Collection", amount: "0.00", identifier: "collection", detail: "Collect from the store" }
                ],
                supportedCountries: [countryCode]
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

            // Create the Apple Pay session.
            var session = new ApplePaySession(10, paymentRequest);

            // Setup handler for validation the merchant session.
            session.onvalidatemerchant = function (event) {

                // Create the payload.
                var data = {
                    validationUrl: event.validationURL
                };

                // Setup antiforgery HTTP header.
                var antiforgeryHeader = $("meta[name='x-antiforgery-name']").attr("content");
                var antiforgeryToken = $("meta[name='x-antiforgery-token']").attr("content");

                var headers = {};
                headers[antiforgeryHeader] = antiforgeryToken;

                // Post the payload to the server to validate the
                // merchant session using the merchant certificate.
                $.ajax({
                    url: $("link[rel='merchant-validation']").attr("href"),
                    method: "POST",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify(data),
                    headers: headers
                }).then(function (merchantSession) {
                    // Complete validation by passing the merchant session to the Apple Pay session.
                    session.completeMerchantValidation(merchantSession);
                });
            };

            // Setup handler for shipping method selection.
            session.onshippingmethodselected = function (event) {

                var newTotal;
                var newLineItems;

                if (event.shippingMethod.identifier === "collection") {
                    newTotal = totalForCollection;
                    newLineItems = lineItemsForCollection;
                } else {
                    newTotal = totalForDelivery;
                    newLineItems = lineItemsForDelivery;
                }

                var update = {
                    newTotal: newTotal,
                    newLineItems: newLineItems
                };

                session.completeShippingMethodSelection(update);
            };

            // Setup handler to receive the token when payment is authorized.
            session.onpaymentauthorized = function (event) {

                // Get the contact details for use, for example to
                // use to create an account for the user.
                var billingContact = event.payment.billingContact;
                var shippingContact = event.payment.shippingContact;

                // Get the payment data for use to capture funds from
                // the encrypted Apple Pay token in your server.
                var token = event.payment.token.paymentData;

                // Apply the details from the Apple Pay sheet to the page.
                var update = function (panel, contact) {

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
                };

                $(".card-name").text(event.payment.token.paymentMethod.displayName);
                update($("#billing-contact"), billingContact);
                update($("#shipping-contact"), shippingContact);

                var authorizationResult = {
                    status: ApplePaySession.STATUS_SUCCESS,
                    errors: []
                };

                // Do something with the payment to capture funds and
                // then dismiss the Apple Pay sheet for the session with
                // the relevant status code for the payment's authorization.
                session.completePayment(authorizationResult);

                justEat.applePay.showSuccess();
            };

            // Start the session to display the Apple Pay sheet.
            session.begin();
        },
        setupApplePay: function () {
            var merchantIdentifier = justEat.applePay.getMerchantIdentifier();
            ApplePaySession.openPaymentSetup(merchantIdentifier)
                .then(function (success) {
                    if (success) {
                        justEat.applePay.hideSetupButton();
                        justEat.applePay.showButton();
                    } else {
                        justEat.applePay.showError("Failed to set up Apple Pay.");
                    }
                }).catch(function (e) {
                    justEat.applePay.showError("Failed to set up Apple Pay. " + e);
                });
        },
        showButton: function () {
            var button = $("#apple-pay-button");
            button.attr("lang", justEat.applePay.getPageLanguage());
            button.on("click", justEat.applePay.beginPayment);

            if (justEat.applePay.supportsSetup()) {
                button.addClass("apple-pay-button-with-text");
                button.addClass("apple-pay-button-black-with-text");
            } else {
                button.addClass("apple-pay-button");
                button.addClass("apple-pay-button-black");
            }

            button.removeClass("d-none");
        },
        showSetupButton: function () {
            var button = $("#set-up-apple-pay-button");
            button.attr("lang", justEat.applePay.getPageLanguage());
            button.on("click", $.proxy(justEat.applePay, "setupApplePay"));
            button.removeClass("d-none");
        },
        hideSetupButton: function () {
            var button = $("#set-up-apple-pay-button");
            button.addClass("d-none");
            button.off("click");
        },
        showError: function (text) {
            var error = $(".apple-pay-error");
            error.text(text);
            error.removeClass("d-none");
        },
        showSuccess: function () {
            $(".apple-pay-intro").hide();
            var success = $(".apple-pay-success");
            success.removeClass("d-none");
        },
        supportedByDevice: function () {
            return "ApplePaySession" in window;
        },
        supportsSetup: function () {
            return "openPaymentSetup" in ApplePaySession;
        },
        getPageLanguage: function () {
            return $("html").attr("lang") || "en";
        },
        getMerchantIdentifier: function () {
            return $("meta[name='apple-pay-merchant-id']").attr("content");
        }
    }
};

(function () {

    // Get the merchant identifier from the page meta tags.
    var merchantIdentifier = justEat.applePay.getMerchantIdentifier();

    if (!merchantIdentifier) {
        justEat.applePay.showError("No Apple Pay merchant certificate is configured.");
    }
    // Is ApplePaySession available in the browser?
    else if (justEat.applePay.supportedByDevice()) {

        // Determine whether to display the Apple Pay button. See this link for details
        // on the two different approaches: https://developer.apple.com/documentation/applepayjs/checking_if_apple_pay_is_available
        if (ApplePaySession.canMakePayments() === true) {
            justEat.applePay.showButton();
        } else {
            ApplePaySession.canMakePaymentsWithActiveCard(merchantIdentifier).then(function (canMakePayments) {
                if (canMakePayments === true) {
                    justEat.applePay.showButton();
                } else {
                    if (justEat.applePay.supportsSetup()) {
                        justEat.applePay.showSetupButton(merchantIdentifier);
                    } else {
                        justEat.applePay.showError("Apple Pay cannot be used at this time. If using macOS you need to be paired with a device that supports at least TouchID.");
                    }
                }
            });
        }
    } else {
        justEat.applePay.showError("This device and/or browser does not support Apple Pay.");
    }
})();
