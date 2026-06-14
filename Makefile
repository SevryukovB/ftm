run:
	docker compose up --build -d
	@echo ""
	@echo "  App:     http://localhost:8080"
	@echo "  API:     http://localhost:5080/swagger"
	@echo "  Admin:   admin@ftm.local / Admin123!"

logs:
	docker compose logs -f

stop:
	docker compose down

clean:
	docker compose down -v --rmi local

.PHONY: run logs stop clean
