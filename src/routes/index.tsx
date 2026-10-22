import { createFileRoute } from "@tanstack/react-router";
import { DerbyGame } from "@/components/derby/DerbyGame";

export const Route = createFileRoute("/")({ component: DerbyGame });
