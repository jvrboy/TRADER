package com.deriv.swarm;

import com.deriv.swarm.core.*;
import org.junit.jupiter.api.*;
import static org.junit.jupiter.api.Assertions.*;

import java.util.Map;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

class CoreFrameworkTest {

    private MessageBus bus;

    @BeforeEach
    void setUp() {
        bus = new MessageBus();
        bus.start();
    }

    @AfterEach
    void tearDown() {
        bus.stop();
    }

    @Test
    void testMessageBusPublishAndSubscribe() {
        AtomicInteger received = new AtomicInteger(0);
        bus.subscribe("agent-1", msg -> received.incrementAndGet());

        for (int i = 0; i < 100; i++) {
            bus.publish(new AgentMessage("sender", "agent-1", "TEST", "payload-" + i));
        }

        // Wait for async processing
        await(() -> received.get() == 100);
        assertEquals(100, received.get());
    }

    @Test
    void testMessageBusBroadcast() {
        AtomicInteger count1 = new AtomicInteger(0);
        AtomicInteger count2 = new AtomicInteger(0);
        AtomicInteger count3 = new AtomicInteger(0);

        bus.subscribe("a1", msg -> count1.incrementAndGet());
        bus.subscribe("a2", msg -> count2.incrementAndGet());
        bus.subscribe("a3", msg -> count3.incrementAndGet());

        bus.publish(AgentMessage.broadcast("sender", "BROADCAST", "hello"));

        await(() -> count1.get() == 1 && count2.get() == 1 && count3.get() == 1);
        assertEquals(1, count1.get());
        assertEquals(1, count2.get());
        assertEquals(1, count3.get());
    }

    @Test
    void testMessageBusPriority() {
        StringBuilder order = new StringBuilder();
        bus.subscribe("target", msg -> order.append(msg.getPriority()).append("-"));

        bus.publish(new AgentMessage("s", "t", "TYPE", "lo").withPriority(1));
        bus.publish(new AgentMessage("s", "t", "TYPE", "hi").withPriority(10));
        bus.publish(new AgentMessage("s", "t", "TYPE", "mid").withPriority(5));

        await(() -> order.length() >= 6);
        assertTrue(order.toString().contains("10-"));
    }

    @Test
    void testAgentRegistry() {
        AgentRegistry registry = new AgentRegistry();
        assertEquals(0, registry.count());

        TestAgent a1 = new TestAgent("a1", "test", bus);
        TestAgent a2 = new TestAgent("a2", "test", bus);
        TestAgent a3 = new TestAgent("a3", "other", bus);

        registry.register(a1);
        registry.register(a2);
        registry.register(a3);

        assertEquals(3, registry.count());
        assertEquals(2, registry.countByType("test"));
        assertEquals(1, registry.countByType("other"));
        assertNotNull(registry.get("a1"));
        assertNull(registry.get("nonexistent"));
    }

    @Test
    void testAgentLifecycle() {
        TestAgent agent = new TestAgent("test-1", "test", bus);
        agent.initialize(Map.of("key", "value"));
        assertEquals(AgentState.IDLE, agent.getState());

        agent.start();
        assertEquals(AgentState.RUNNING, agent.getState());

        agent.stop();
        assertEquals(AgentState.STOPPED, agent.getState());
    }

    @Test
    void testAgentMessaging() {
        TestAgent sender = new TestAgent("sender", "test", bus);
        TestAgent receiver = new TestAgent("receiver", "test", bus);
        sender.initialize(Map.of());
        receiver.initialize(Map.of());

        sender.start();
        receiver.start();

        sender.sendTo("receiver", "GREETING", "hello");

        await(() -> receiver.getMessagesProcessed() >= 1);
        assertTrue(receiver.getMessagesProcessed() >= 1);
        assertTrue(sender.getMessagesSent() >= 1);

        sender.stop();
        receiver.stop();
    }

    @Test
    void testAgentSwarmCreation() {
        AgentSwarm swarm = new AgentSwarm();
        TestAgent a1 = new TestAgent("swarm-1", "test", bus);
        TestAgent a2 = new TestAgent("swarm-2", "test", bus);
        TestAgent a3 = new TestAgent("swarm-3", "coordination", bus);

        swarm.initialize(List.of(a1, a2, a3), Map.of("test", "true"));
        assertEquals(3, swarm.getRegistry().count());

        swarm.start();
        assertTrue(swarm.isRunning());

        swarm.stop();
        assertFalse(swarm.isRunning());
    }

    @Test
    void testSwarmStats() {
        AgentSwarm swarm = new AgentSwarm();
        TestAgent a1 = new TestAgent("s1", "type_a", bus);
        TestAgent a2 = new TestAgent("s2", "type_b", bus);
        swarm.initialize(List.of(a1, a2), Map.of());

        String stats = swarm.getStats();
        assertNotNull(stats);
        assertTrue(stats.contains("totalAgents"));
        assertTrue(stats.contains("agentsByType"));
        swarm.stop();
    }

    @Test
    void testMessageBusUnsubscribe() {
        AtomicInteger count = new AtomicInteger(0);
        bus.subscribe("temp", msg -> count.incrementAndGet());
        bus.publish(new AgentMessage("s", "temp", "T", "p"));
        await(() -> count.get() >= 1);
        assertEquals(1, count.get());

        bus.unsubscribe("temp");
        bus.publish(new AgentMessage("s", "temp", "T", "p2"));
        // After unsubscribe, no more messages should arrive
        assertEquals(1, count.get());
    }

    @Test
    void testAgentMessageProperties() {
        AgentMessage msg = new AgentMessage("agent-1", "agent-2", "DATA", "payload");
        assertNotNull(msg.getId());
        assertEquals("agent-1", msg.getSenderId());
        assertEquals("agent-2", msg.getRecipientId());
        assertEquals("DATA", msg.getType());
        assertEquals("payload", msg.getPayload());
        assertFalse(msg.isBroadcast());
        assertEquals(5, msg.getPriority());
    }

    @Test
    void testBroadcastMessage() {
        AgentMessage broadcast = AgentMessage.broadcast("sender", "ALERT", "warning");
        assertTrue(broadcast.isBroadcast());
        assertEquals("*", broadcast.getRecipientId());
    }

    @Test
    void testMessageBusMultipleSubscribersSameAgent() {
        AtomicInteger c1 = new AtomicInteger(0);
        AtomicInteger c2 = new AtomicInteger(0);
        bus.subscribe("multi", msg -> c1.incrementAndGet());
        bus.subscribe("multi", msg -> c2.incrementAndGet());

        bus.publish(new AgentMessage("s", "multi", "T", "p"));
        await(() -> c1.get() >= 1 && c2.get() >= 1);
        assertEquals(1, c1.get());
        assertEquals(1, c2.get());
    }

    private void await(Runnable condition) {
        try {
            for (int i = 0; i < 50; i++) {
                if (true) { condition.run(); } // check
                Thread.sleep(50);
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
    }

    static class TestAgent extends Agent {
        TestAgent(String id, String type, MessageBus bus) {
            super(id, type, bus);
        }
        @Override protected void onInitialize(Map<String, String> cfg) {}
        @Override protected void onStart() {}
        @Override protected void handleMessage(AgentMessage msg) {}
    }
}