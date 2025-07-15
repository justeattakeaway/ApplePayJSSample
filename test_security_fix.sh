#!/bin/bash

# Security Fix Verification Script
# Tests that SSRF vulnerability has been properly fixed

echo "🔒 Apple Pay SSRF Security Fix Verification"
echo "=========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

BASE_URL="https://localhost:5001"  # Adjust this to your test environment
VALIDATE_ENDPOINT="$BASE_URL/applepay/validate"

echo -e "${YELLOW}Testing SSRF vulnerability fixes...${NC}"
echo ""

# Test 1: Legitimate Apple Pay domain (should work)
echo "✅ Test 1: Legitimate Apple Pay domain"
curl -s -X POST "$VALIDATE_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "https://apple-pay-gateway-cert.apple.com/paymentservices/startSession"}' \
  -w "HTTP Status: %{http_code}\n" \
  | head -1

echo ""

# Test 2: AWS Metadata attack (should be blocked)
echo "🚫 Test 2: AWS Metadata Service Attack (should be blocked)"
response=$(curl -s -X POST "$VALIDATE_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "http://169.254.169.254/latest/meta-data/"}' \
  -w "HTTP Status: %{http_code}")

if [[ $response == *"400"* ]]; then
    echo -e "${GREEN}✅ BLOCKED - AWS metadata attack prevented${NC}"
else
    echo -e "${RED}❌ VULNERABLE - AWS metadata attack not blocked${NC}"
fi

echo ""

# Test 3: Internal network scan (should be blocked)
echo "🚫 Test 3: Internal Network Scan (should be blocked)"
response=$(curl -s -X POST "$VALIDATE_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "http://10.0.0.1:8080/admin"}' \
  -w "HTTP Status: %{http_code}")

if [[ $response == *"400"* ]]; then
    echo -e "${GREEN}✅ BLOCKED - Internal network scan prevented${NC}"
else
    echo -e "${RED}❌ VULNERABLE - Internal network scan not blocked${NC}"
fi

echo ""

# Test 4: Database access attempt (should be blocked)
echo "🚫 Test 4: Database Access Attempt (should be blocked)"
response=$(curl -s -X POST "$VALIDATE_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "http://localhost:5432/"}' \
  -w "HTTP Status: %{http_code}")

if [[ $response == *"400"* ]]; then
    echo -e "${GREEN}✅ BLOCKED - Database access attempt prevented${NC}"
else
    echo -e "${RED}❌ VULNERABLE - Database access attempt not blocked${NC}"
fi

echo ""

# Test 5: File system access (should be blocked)
echo "🚫 Test 5: File System Access (should be blocked)"
response=$(curl -s -X POST "$VALIDATE_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "file:///etc/passwd"}' \
  -w "HTTP Status: %{http_code}")

if [[ $response == *"400"* ]]; then
    echo -e "${GREEN}✅ BLOCKED - File system access prevented${NC}"
else
    echo -e "${RED}❌ VULNERABLE - File system access not blocked${NC}"
fi

echo ""

# Test 6: Non-HTTPS Apple domain (should be blocked)
echo "🚫 Test 6: Non-HTTPS Apple Domain (should be blocked)"
response=$(curl -s -X POST "$VALIDATE_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "http://apple-pay-gateway.apple.com/paymentservices/startSession"}' \
  -w "HTTP Status: %{http_code}")

if [[ $response == *"400"* ]]; then
    echo -e "${GREEN}✅ BLOCKED - Non-HTTPS request prevented${NC}"
else
    echo -e "${RED}❌ VULNERABLE - Non-HTTPS request not blocked${NC}"
fi

echo ""
echo "=========================================="
echo -e "${YELLOW}Security fix verification complete!${NC}"
echo ""
echo "🔍 Expected Results:"
echo "  ✅ Test 1 should work (legitimate Apple Pay domain)"
echo "  🚫 Tests 2-6 should be blocked (SSRF attempts)"
echo ""
echo "📝 If any SSRF tests show as VULNERABLE, the security fix needs review."
echo "🛡️  All tests showing BLOCKED indicate successful SSRF prevention."
